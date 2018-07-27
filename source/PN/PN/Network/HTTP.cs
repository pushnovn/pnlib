using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static PN.Network.HTTP.Entities;

namespace PN.Network
{
    public class HTTP
    {
        protected static dynamic Base(RequestEntity requestModel)
        {
            var methodInfo = GetMethodInfo();

            return TryExecuteAction(methodInfo.ReturnType, () =>
            {
                #region URL and request type

                var requestUri = new Uri(
                                    BaseUrl +
                                    methodInfo.ClassName.ToLower() + "/" +
                                    ProcessComplexString(methodInfo.Url, requestModel));

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
                request.Method = methodInfo.RequestType.ToString();

                #endregion

                #region Headers

                var headers = methodInfo.IgnoreGlobalHeaders ? new List<HeaderAttribute>() : GlobalHeaders;
                headers.AddRange(methodInfo.HeaderAttributes ?? new List<HeaderAttribute>());

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

                using (WebResponse response = request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        var responseJson = new StreamReader(responseStream).ReadToEnd();
                        var responseObject = JsonConvert.DeserializeObject(responseJson, methodInfo.ReturnType);

                        return Convert.ChangeType(responseObject, methodInfo.ReturnType);
                    }
                }

                #endregion
            });
        }

        private static dynamic TryExecuteAction(Type responseModelType, Func<object> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                var instance = Activator.CreateInstance(responseModelType);
                responseModelType.GetProperty(nameof(ResponseEntity.Exception)).SetValue(instance, ex);
                return instance;
            }
        }
        
        private static ReflMethodInfo GetMethodInfo()
        {
            StackTrace st = new StackTrace();
            StackFrame[] fr = st.GetFrames();

            if (fr == null) return null;
            
            var method = fr[2].GetMethod();

            var url = method.GetCustomAttributes()?.OfType<UrlAttribute>()?.FirstOrDefault();
            var requestType = method.GetCustomAttributes()?.OfType<RequestTypeAttribute>()?.FirstOrDefault();
            var contentType = method.GetCustomAttributes()?.OfType<ContentTypeAttribute>()?.FirstOrDefault();
            var ignoreGlobalHeaders = method.GetCustomAttributes()?.OfType<IgnoreGlobalHeadersAttribute>()?.FirstOrDefault();
            var headers = method.GetCustomAttributes()?.OfType<HeaderAttribute>()?.ToList();

            return new ReflMethodInfo()
            {
                ReturnType = method is MethodInfo ? (method as MethodInfo).ReturnType : null,
                Name = method.Name,
                ClassName = method.ReflectedType.Name,
                Url = url?.Url,
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

                str += model.GetType().GetProperty(m.Value.Substring(1, m.Length - 2)).GetValue(model) as String ?? m.Value;

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
            internal string ClassName { get; set; }
            internal Type ReturnType { get; set; }

            internal string Url { get; set; }
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
        protected class HeaderAttribute : Attribute
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
            public class RequestEntity { }
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
        protected static string BaseUrl
        {
            get => _baseUrl ?? throw new ArgumentException("Base URL is not set!");
            set => _baseUrl = string.IsNullOrWhiteSpace(value) ? null : value.TrimEnd('/') + '/';
        }

        protected static List<HeaderAttribute> GlobalHeaders { get; set; } = new List<HeaderAttribute>();

        protected static void Init(string baseUrl, List<HeaderAttribute> headers)
        {
            BaseUrl = baseUrl;
            GlobalHeaders = headers ?? new List<HeaderAttribute>();
        }

        #endregion
    }
}