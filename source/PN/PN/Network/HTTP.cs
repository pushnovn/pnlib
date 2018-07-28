using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using static PN.Network.HTTP.Entities;

namespace PN.Network
{
    public class HTTP
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected static dynamic Base(RequestEntity requestModel)
        {
            var methodInfo = GetMethodInfo();

            var method = typeof(HTTP).GetMethod(nameof(BaseAsyncPrivate), BindingFlags.NonPublic | BindingFlags.Static);
            var generic = method.MakeGenericMethod(methodInfo.ReturnType);
            var task = generic.Invoke(null, new object[] { requestModel, methodInfo });
            
            return methodInfo.IsGenericType ? task : task.GetType().GetProperty(nameof(Task<dynamic>.Result)).GetValue(task, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<T> BaseAsyncPrivate<T>(RequestEntity requestModel, ReflMethodInfo methodInfo)
        {
            //if (propertyName != null)
            //    Console.WriteLine("propertyName = " + propertyName);
            //Console.WriteLine("current method:");
            //var mm = MethodBase.GetCurrentMethod();
            //Console.WriteLine(mm.ReflectedType.DeclaringType);

            //       Console.WriteLine(JsonConvert.SerializeObject( WWWW_CustomAttribute.ppp));
            
            try
            {
                #region URL and request type

            //    Console.WriteLine(methodInfo.MethodFullUrl);
                var requestUri = new Uri(ProcessComplexString(methodInfo.MethodFullUrl, requestModel));
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
                request.Method = methodInfo.RequestType.ToString();

                #endregion

                #region Headers

                var headers = new List<HeaderAttribute>();
                headers.AddRange(methodInfo.IgnoreGlobalHeaders ? new List<HeaderAttribute>() : GlobalHeaders);
                headers.AddRange(methodInfo.HeaderAttributes ?? new List<HeaderAttribute>());
                headers.AddRange(requestModel.Headers ?? new List<HeaderAttribute>());

                foreach (var header in headers)
                {
                    if (string.IsNullOrWhiteSpace(header.Key) || string.IsNullOrWhiteSpace(header.Value))
                        continue;

                    request.Headers[header.Key] = ProcessComplexString(header.Value, requestModel);
                }

                #endregion

                #region Body

                var requestJson = JsonConvert.SerializeObject(requestModel);
                if (!requestJson.Equals("{}") && !requestJson.Equals("null"))
                {
                    byte[] requestBody = Encoding.UTF8.GetBytes(requestJson);

                    request.ContentLength = requestBody.Length;
                    request.ContentType = ContentTypeToString(methodInfo.ContentType);

                    using (Stream stream = request.GetRequestStream())
                        stream.Write(requestBody, 0, requestBody.Length);
                }

                #endregion

                #region Execute request

                using (WebResponse response = await request.GetResponseAsync())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        var responseJson = new StreamReader(responseStream).ReadToEnd();
                        var responseObject = JsonConvert.DeserializeObject(responseJson, methodInfo.ReturnType);

                        return (T)Convert.ChangeType(responseObject, methodInfo.ReturnType);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                var instance = Activator.CreateInstance(methodInfo.ReturnType);
                methodInfo.ReturnType.GetProperty(nameof(ResponseEntity.Exception)).SetValue(instance, ex);
                return (T)instance;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ReflMethodInfo GetMethodInfo()
        {
            StackTrace st = new StackTrace();
            StackFrame[] fr = st.GetFrames();
            if (fr == null) return null;
            
            #region debug comments

            //StackTrace st2 = new StackTrace(true);
            //for (int i = 0; i < st2.FrameCount; i++)
            //{
            //    // Note that high up the call stack, there is only
            //    // one stack frame.
            //    StackFrame sf = st2.GetFrame(i);

            //    Console.WriteLine();
            //    Console.WriteLine("High up the call stack, Method: {0}", sf.GetMethod());
            //    Console.WriteLine("High up the call stack, Line Number: {0}", sf.GetFileLineNumber());

            //    var meth = sf.GetMethod();
            //    var retType = (meth as MethodInfo)?.ReturnType;

            //    //    var ignoreGlobalHeader2222222s = sf.GetMethod().GetCustomAttributes()?.OfType<IgnoreGlobalHeadersAttribute>()?.FirstOrDefault();
            //    var attrs = sf.GetMethod().GetCustomAttributes();
            //    if (attrs != null)
            //    {
            //        Console.WriteLine("GetCustomAttributes COUNT: {0}", attrs.Count());
            //        foreach (var att in attrs)
            //        {
            //            Console.WriteLine("Attr: {0}", att.GetType().Name);
            //        }
            //    }
            //    Console.WriteLine("ReturnType: {0}", retType);
            //}

            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();
            //foreach (var f in fr)
            //    Console.WriteLine(JsonConvert.SerializeObject(f.GetMethod() as MethodInfo));

            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();

            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();

            //foreach (var f in fr)
            //    Console.WriteLine(JsonConvert.SerializeObject(f.GetMethod()));

            #endregion
            
            var method = fr[2].GetMethod();

            var baseResponseModelType = (method as MethodInfo)?.ReturnType;
            var isGenericType = baseResponseModelType.IsGenericType;
            var responseModelType = isGenericType ? baseResponseModelType.GetGenericArguments()[0] : baseResponseModelType;
           
            var url = method.GetCustomAttributes()?.OfType<UrlAttribute>()?.FirstOrDefault();
            var requestType = method.GetCustomAttributes()?.OfType<RequestTypeAttribute>()?.FirstOrDefault();
            var contentType = method.GetCustomAttributes()?.OfType<ContentTypeAttribute>()?.FirstOrDefault();
            var ignoreGlobalHeaders = method.GetCustomAttributes()?.OfType<IgnoreGlobalHeadersAttribute>()?.FirstOrDefault();
            var headers = method.GetCustomAttributes()?.OfType<HeaderAttribute>()?.ToList();
            
            var temp_uri = string.Empty;
            var typ = method.ReflectedType;
            while (typ != null)
            {
                var att = typ
                    .GetCustomAttributes(typeof(UrlAttribute), true)
                    .FirstOrDefault() as UrlAttribute;

                var checkBaseUrl = typ.ReflectedType == null && string.IsNullOrWhiteSpace(_baseUrl) == false;
                temp_uri = (checkBaseUrl ? BaseUrl : (att?.Url ?? typ.Name).Trim('/') + "/") + temp_uri;
                
                typ = typ.ReflectedType;
            }

            return new ReflMethodInfo()
            {
                ReturnType = responseModelType,// typeof(T) != typeof(object) ? typeof(T) : (method as MethodInfo)?.ReturnType,
                BaseReturnType = baseResponseModelType,
                IsGenericType = isGenericType,
                Name = method.Name,
                MethodPath = temp_uri,
                MethodPartialUrl = url?.Url,
                RequestType = requestType == null ? RequestTypes.POST : requestType.RequestType,
                ContentType = contentType == null ? ContentTypes.JSON : contentType.ContentType,
                IgnoreGlobalHeaders = ignoreGlobalHeaders != null,
                HeaderAttributes = headers,
            };
        }

        private static string ProcessComplexString(string strToProcess, RequestEntity model)
        {
            if (string.IsNullOrWhiteSpace(strToProcess))
                return string.Empty;

            MatchCollection matches = Regex.Matches(strToProcess, @"\{[\w]+\}");

            var str = strToProcess.Substring(0, matches.Count > 0 ? matches[0].Index : strToProcess.Length);

            for (int i = 0; i < matches.Count; i++)
            {
                Match m = matches[i];

                str += model.GetType().GetProperty(m.Value.Substring(1, m.Length - 2))?.GetValue(model) as String ?? m.Value;

                var indexOfLastMatchChar = m.Index + m.Length;

                var nextClearPartLength = -indexOfLastMatchChar + (i + 1 < matches.Count ? matches[i + 1].Index : strToProcess.Length);

                str += strToProcess.Substring(indexOfLastMatchChar, nextClearPartLength);
            }

            return str;
        }

        private static string ContentTypeToString(ContentTypes contentType)
        {
            switch (contentType)
            {
                case ContentTypes.JSON:
                    return "application/json";

                default:
                    return "application/json";
            } 
        }

        private class ReflMethodInfo
        {
            internal string Name { get; set; }
            internal string MethodPath { get; set; }
            internal Type ReturnType { get; set; }
            internal Type BaseReturnType { get; set; }
            internal bool IsGenericType { get; set; }

            internal string MethodPartialUrl { get; set; }
            internal string MethodFullUrl => (MethodPath ?? "") + (MethodPartialUrl ?? "");
            internal RequestTypes RequestType { get; set; }
            internal ContentTypes ContentType { get; set; }
            internal List<HeaderAttribute> HeaderAttributes { get; set; }
            internal bool IgnoreGlobalHeaders { get; set; }
        }

        #region Attributes
        
        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        protected class UrlAttribute : Attribute
        {
            public readonly string Url;
            public UrlAttribute(string name) { Url = name; }
        }

        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        protected class RequestTypeAttribute : Attribute
        {
            public readonly RequestTypes RequestType;
            public RequestTypeAttribute(RequestTypes requestType) { RequestType = requestType; }
        }

        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        protected class ContentTypeAttribute : Attribute
        {
            public readonly ContentTypes ContentType;
            public ContentTypeAttribute(ContentTypes contentType) { ContentType = contentType; }
        }

        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
        public class HeaderAttribute : Attribute
        {
            public readonly string Key, Value;
            public HeaderAttribute(string key, string value) { Key = key; Value = value; }
        }

        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        protected class IgnoreGlobalHeadersAttribute : Attribute { }

        #endregion

        protected enum RequestTypes { GET, POST, PUT, DELETE }
        protected enum ContentTypes { JSON }

        public class Entities
        {
            public class RequestEntity
            {
                [JsonIgnoreAttribute]
                public List<HeaderAttribute> Headers { get; set; }
            }

            public class ResponseEntity
            {
                [JsonIgnoreAttribute]
                public int HttpCode { get; set; }
                
                public Exception Exception { get; set; }
                
                public int ErrorCode { get; set; }
                
                public string ErrorMessage { get; set; }
            }
        }

        #region Props and fields
        
        private static string _baseUrl;
        private static string BaseUrl
        {
            get => _baseUrl ?? throw new ArgumentException("Base URL is not set!");
            set => _baseUrl = string.IsNullOrWhiteSpace(value) ? null : value.TrimEnd('/') + '/';
        }

        protected static List<HeaderAttribute> GlobalHeaders { get; set; } = new List<HeaderAttribute>();

        public static void Init(string baseUrl, List<HeaderAttribute> headers = null)
        {
            BaseUrl = baseUrl ?? _baseUrl;
            GlobalHeaders = headers ?? GlobalHeaders ?? new List<HeaderAttribute>();
        }
        
        #endregion
    }
    //public class WWWW_CustomAttribute : Attribute
    //{
    //    public static List<MethodInfo> MethodsList = new List<MethodInfo>();
    //    static WWWW_CustomAttribute()
    //    {
    //        var methods = Assembly.GetEntryAssembly() //Use .GetCallingAssembly() if this method is in a library, or even both
    //                    .GetTypes()
    //                    .SelectMany(t => t.GetMethods())
    //        //          .Where(m => m.GetCustomAttributes(typeof(WWWW_CustomAttribute), false).Length > 0)
    //                    .ToArray();

    //        MethodsList.AddRange(methods);

    //        ppp.Add(Assembly.GetExecutingAssembly().FullName);
    //        Console.WriteLine("GetCallingAssembly: " + JsonConvert.SerializeObject(Assembly.GetCallingAssembly()));

    //    }

    //    public string fullMethodPath;
    //    public bool someThing;
    //    public static List<string> ppp = new List<string>();

    //    public WWWW_CustomAttribute([CallerMemberName] string membername = "")
    //    {
    //        //var methods = Assembly.GetExecutingAssembly() //Use .GetCallingAssembly() if this method is in a library, or even both
    //        //          .GetTypes()
    //        //          .SelectMany(t => t.GetMethods())
    //        //        //  .Where(m => m.GetCustomAttributes(typeof(WWWW_CustomAttribute), false).Length > 0)
    //        //          .ToArray();
    //        //MethodsList.AddRange(methods);
    //        //  ppp.Add(Assembly.GetCallingAssembly().FullName);
    //        //    Console.WriteLine(Assembly.GetCallingAssembly().FullName);




    //        var method = MethodsList.FirstOrDefault(m => m.Name == membername);
    //            if (method == null || method.DeclaringType == null) return; //Not suppose to happen, but safety comes first
    //            fullMethodPath = method.DeclaringType.Name + membername; //Work it around any way you want it
    //                                                                     //  I need here to get the type of membername parent. 
    //                                                                     //  Here I want to get CustClass, not fooBase
    //        }
    //    }
}