using InternalNewtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static PN.Network.HTTP.Entities;


/// <summary>
/// Network library
/// </summary>
namespace PN.Network
{
    /// <summary>
    /// Class from which you must inherit your custom class.
    /// <para>Inherit class supports Url attribute: [Url("https://you_base_url.com")]</para>
    /// </summary>
    public class HTTP
    {
        /// <summary>
        /// Docs (RU) avaliable on http://wiki.pushnovn.com/doku.php?id=csharp_pn_lib_network_http
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected static dynamic Base(RequestEntity requestModel = null) => CreatePrivateBase(requestModel, GetMethodInfo());


        
        /// <summary>
        /// All downloading data will be stored at RAM. If you want change this, set flushBuffer value to true, for clearing memory after downloading part of data. 
        /// </summary>
        /// <returns>Method will return object that you specified as generic type. If you set <typeparamref name="TResponse"/> as Task type, it would return Task and may be used as async/await.</returns>
        public static TResponse Request<TResponse>(string url, RequestEntity requestEntity = null, params object[] settings)
        {
            var headers = settings.OfType<HeaderAttribute>()?.ToList();
            settings.OfType<IEnumerable<HeaderAttribute>>()?.ToList()?.ForEach(l => headers.AddRange(l));

            var methodInfo = new ReflMethodInfo()
            {
                MethodPath = url,
                BaseReturnType = typeof(TResponse),
                IsGenericType = typeof(TResponse).IsGenericType,
                ReturnType = typeof(TResponse).IsGenericType ? typeof(TResponse).GetGenericArguments()[0] : typeof(TResponse),
                RequestType = settings.OfType<RequestTypes>().LastOrDefault(),
                ContentType = settings.OfType<ContentTypes>().LastOrDefault(),
                IgnoreGlobalHeaders = settings.OfType<IgnoreGlobalHeadersAttribute>()?.Count() > 0,
                HeaderAttributes = headers,
                UserAgentString = settings.OfType<UserAgentAttribute>()?.LastOrDefault()?.UserAgentString,
            };

            return (TResponse) CreatePrivateBase(requestEntity, methodInfo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object CreatePrivateBase(RequestEntity requestModel, ReflMethodInfo methodInfo)
        {
            var method = typeof(HTTP).GetMethod(nameof(BaseAsyncPrivate), BindingFlags.NonPublic | BindingFlags.Static);
            var generic = method?.MakeGenericMethod(methodInfo.ReturnType);
            var task = generic?.Invoke(null, new object[] {requestModel, methodInfo});

            return methodInfo.IsGenericType ? task : task.GetType().GetProperty(nameof(Task<dynamic>.Result)).GetValue(task, null);
        }

        private static async Task<T> BaseAsyncPrivate<T>(RequestEntity requestModel, ReflMethodInfo methodInfo)
        {
            try
            {
                #region URL and request type

                requestModel = requestModel ?? new RequestEntity();

                var requestUri = new Uri(Utils.Utils.Internal.ProcessComplexString(methodInfo.MethodFullUrl, requestModel));
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(requestUri);

                request.Method = methodInfo.RequestType.ToString();
                request.ContentType = ContentTypeToString(methodInfo.ContentType);
                request.UserAgent = methodInfo.UserAgentString;
                request.Timeout = requestModel.Timeout ?? request.Timeout;

                #endregion

                #region Headers

                var headers = new List<HeaderAttribute>();
                headers.AddRange(methodInfo.IgnoreGlobalHeaders ? new List<HeaderAttribute>() : GlobalHeaders);
                headers.AddRange(methodInfo.HeaderAttributes ?? new List<HeaderAttribute>());
                headers.AddRange(requestModel.Headers ?? new List<HeaderAttribute>());

                foreach (var header in headers)
                {
                    if (string.IsNullOrWhiteSpace(header.Key) == false)
                    {
                        request.Headers[header.Key] =
                            Utils.Utils.Internal.ProcessComplexString(header.Value, requestModel);
                    }
                }

                #endregion

                #region Body

                byte[] requestBody = requestModel.Body ?? null;

                var requestJson = JsonConvert.SerializeObject(requestModel);
                if (requestBody == null && methodInfo.RequestType != RequestTypes.GET && !requestJson.Equals("{}") &&
                    !requestJson.Equals("null"))
                {
                    requestBody = Encoding.UTF8.GetBytes(requestJson);
                }

                if (requestBody != null)
                {
                    request.ContentLength = requestBody.Length;
                    using (Stream stream = request.GetRequestStream())
                    {
                        stream.Write(requestBody, 0, requestBody.Length);
                    }
                }

                #endregion

                #region Execute request

                using (var response = (HttpWebResponse) await request.GetResponseAsync())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        var responseBody = await GetResponseBodyWithProgressAsync(requestModel, response.ContentLength, stream);
                        var responseJson = Encoding.UTF8.GetString(responseBody);
                        LastResponse = new ResponseEntity() {ResponseBody = responseBody, ResponseText = responseJson};

                        object instance;
                        try
                        {
                            if (typeof(T).IsValueType)
                                instance = Utils.Utils.Converters.FromByteArray<T>(responseBody);
                            else if (typeof(T) == typeof(string))
                                instance = responseJson;
                            else if (typeof(T) == typeof(byte[]))
                                instance = responseBody;
                            else
                                instance = JsonConvert.DeserializeObject(responseJson, methodInfo.ReturnType);
                        }
                        catch (Exception exc)
                        {
                            instance = Utils.Utils.Internal.CreateDefaultObject(methodInfo.ReturnType);
                            Utils.Utils.Internal.TrySetValue(ref instance, exc, nameof(ResponseEntity.Exception));
                            LastResponse.Exception = exc;
                        }

                        Utils.Utils.Internal.TrySetValue(ref instance, responseBody, nameof(ResponseEntity.ResponseBody));
                        Utils.Utils.Internal.TrySetValue(ref instance, responseJson, nameof(ResponseEntity.ResponseText));
                        Utils.Utils.Internal.TrySetValue(ref instance, responseJson, nameof(ResponseEntity.ResponseDynamic), true);
                        Utils.Utils.Internal.TrySetValue(ref instance, response.StatusCode, nameof(ResponseEntity.HttpCode));

                        return (T) instance;
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                LastResponse = new ResponseEntity() {Exception = ex};
                var instance = Utils.Utils.Internal.CreateDefaultObject(methodInfo.ReturnType);
                return (T) Utils.Utils.Internal.TrySetValue(ref instance, ex, nameof(ResponseEntity.Exception));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ReflMethodInfo GetMethodInfo()
        {
            var method = new StackTrace().GetFrame(2).GetMethod();

            MemberInfo methodInfo = method;
            var methodName = method.Name;
            var baseResponseModelType = (method as MethodInfo)?.ReturnType;
            var isGenericType = baseResponseModelType.IsGenericType;
            var responseModelType = isGenericType ? baseResponseModelType.GetGenericArguments()[0] : baseResponseModelType;
            
            if (method.IsSpecialName && method.Name.StartsWith("get_"))
            {
                methodName = method.Name.Substring(4);
                methodInfo = method.ReflectedType.GetProperty(methodName);
            }
            
            var url = methodInfo.GetCustomAttributes()?.OfType<UrlAttribute>()?.FirstOrDefault();
            var requestType = methodInfo.GetCustomAttributes()?.OfType<RequestTypeAttribute>()?.FirstOrDefault();
            var contentType = methodInfo.GetCustomAttributes()?.OfType<ContentTypeAttribute>()?.FirstOrDefault();
            var ignoreGlobalHeaders = methodInfo.GetCustomAttributes()?.OfType<IgnoreGlobalHeadersAttribute>()?.FirstOrDefault();
            var headers = methodInfo.GetCustomAttributes()?.OfType<HeaderAttribute>()?.ToList();
            var userAgentString = methodInfo.GetCustomAttributes()?.OfType<UserAgentAttribute>()?.FirstOrDefault()?.UserAgentString;

            var temp_uri = string.Empty;
            var typ = methodInfo.ReflectedType;
            while (typ != null)
            {
                var att = typ.GetCustomAttributes(typeof(UrlAttribute), true).FirstOrDefault() as UrlAttribute;

                var checkBaseUrl = typ.ReflectedType == null && string.IsNullOrWhiteSpace(_baseUrl) == false;
                temp_uri = (checkBaseUrl ? BaseUrl : (att?.Url ?? typ.Name).Trim('/') + "/") + temp_uri;

                if (typ.IsSubclassOf(typeof(HTTP)))
                    break;

                typ = typ.ReflectedType;
            }

            return new ReflMethodInfo()
            {
                ReturnType = responseModelType,
                BaseReturnType = baseResponseModelType,
                IsGenericType = isGenericType,
                MethodPath = temp_uri.TrimEnd('/'),
                MethodPartialUrl = url?.Url ?? methodName,
                RequestType = requestType?.RequestType ?? RequestTypes.GET,
                ContentType = contentType?.ContentType ?? ContentTypes.JSON,
                IgnoreGlobalHeaders = ignoreGlobalHeaders != null,
                HeaderAttributes = headers,
                UserAgentString = userAgentString,
            };
        }

        private static async Task<byte[]> GetResponseBodyWithProgressAsync(RequestEntity requestModel, long responseContentLength, Stream responseStream)
        {
            var list = new List<byte>();
            long totalRecieved = 0;
            var bytes = new byte[BUFFER_SIZE];

            var bytesRead = 0;

            do
            {
                bytesRead = await responseStream?.ReadAsync(bytes, 0, BUFFER_SIZE);
                totalRecieved += bytesRead;

                var args = new DownloadProgressChangedEventArgs
                {
                    ResponseBodyLength = responseContentLength,
                    RecievedBytesCount = bytesRead,
                    TotalRecievedBytesCount = totalRecieved,
                    RecievedBytes = bytes
                };

                requestModel.OnDownloadProgressChangedAction?.Invoke(args);
                DownloadProgressChanged?.Invoke(null, args);

                if (requestModel.FlushBuffer)
                    bytes = new byte[BUFFER_SIZE];
                else
                    list.AddRange(bytes.Take(bytesRead));
            } while (bytesRead > 0);

            return list.ToArray();
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
            internal string MethodPath       { get; set; }
            internal Type   ReturnType       { get; set; }
            internal Type   BaseReturnType   { get; set; }
            internal bool   IsGenericType    { get; set; }

            internal string MethodPartialUrl { get; set; }

            internal string MethodFullUrl => (MethodPath ?? "") +
                                             (!string.IsNullOrWhiteSpace(MethodPath) &&
                                              !string.IsNullOrWhiteSpace(MethodPartialUrl)
                                                 ? "/"
                                                 : "") + (MethodPartialUrl?.Trim('/') ?? "");
            
            internal RequestTypes          RequestType         { get; set; }
            internal ContentTypes          ContentType         { get; set; }
            internal List<HeaderAttribute> HeaderAttributes    { get; set; }
            internal bool                  IgnoreGlobalHeaders { get; set; }
            internal string                UserAgentString     { get; set; }

        }

        #region Attributes

        /// <summary>
        /// Host adress or just sub-url
        /// </summary>
        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        protected class UrlAttribute : Attribute
        {
            public readonly string Url;

            public UrlAttribute(string url)
            {
                Url = url;
            }
        }

        /// <summary>
        /// RequestType attribute class used to decorate Web API request objects
        /// </summary>
        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        protected class RequestTypeAttribute : Attribute
        {
            public readonly RequestTypes RequestType;

            public RequestTypeAttribute(RequestTypes requestType)
            {
                RequestType = requestType;
            }
        }

        /// <summary>
        /// The HTTP MIME type of the output stream. The default value is "json"
        /// </summary>
        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        protected class ContentTypeAttribute : Attribute
        {
            public readonly ContentTypes ContentType;

            public ContentTypeAttribute(ContentTypes contentType)
            {
                ContentType = contentType;
            }
        }

        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
        public class HeaderAttribute : Attribute
        {
            public readonly string Key, Value;

            public HeaderAttribute(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }

        /// <summary>
        /// GlobalHeadersAttribute will be ignored for action where you will define IgnoreHeadersAttribute
        /// </summary>
        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        public class IgnoreGlobalHeadersAttribute : Attribute
        {
        }

        /// <summary>
        /// User-Agent string
        /// </summary>
        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        protected class UserAgentAttribute : Attribute
        {
            public readonly string UserAgentString;

            public UserAgentAttribute(string userAgentString)
            {
                UserAgentString = userAgentString;
            }
        }

        #endregion

        /// <summary>
        /// HTTP defines a set of request methods to indicate the desired action to be performed for a given resource
        /// </summary>
        public enum RequestTypes
        {
            /// <summary>
            /// The GET method requests a representation of the specified resource. Requests using GET should only retrieve data.
            /// </summary>
            GET,

            /// <summary>
            /// The POST method is used to submit an entity to the specified resource, often causing a change in state or side effects on the server.
            /// </summary>
            POST,

            /// <summary>
            /// The PUT method replaces all current representations of the target resource with the request payload.
            /// </summary>
            PUT,

            /// <summary>
            /// The DELETE method deletes the specified resource.
            /// </summary>
            DELETE
        }

        /// <summary>
        /// Is a two-part identifier for file formats and format contents transmitted on the Internet.
        /// </summary>
        public enum ContentTypes
        {
            /// <summary>
            /// application/json: JavaScript Object Notation JSON (RFC 4627)
            /// </summary>
            JSON
        }

        public class Entities
        {
            public class RequestEntity
            {
                /// <summary>
                /// Add custom headers to current request.
                /// </summary>
                [JsonIgnore]
                public List<HeaderAttribute> Headers { get; set; }

                /// <summary>
                /// If Body is not null, request use that Body to push it on server. If request's type is GET, Body is ignoring.
                /// </summary>
                [JsonIgnore]
                public byte[] Body { get; set; }


                /// <summary>
                /// Via Timeout you may set request's timeout. If it null, it's used default timeout.
                /// </summary>
                [JsonIgnore]
                public int? Timeout { get; set; }


                /// <summary>
                /// Flush the buffer after receiving portion of bytes. If true, buffer will be flushed every time after recieving next portion of bytes from server and invoking DownloadProgressChanged event or OnProgressChangedAction action.
                /// </summary>
                [JsonIgnore]
                public bool FlushBuffer { get; set; } = false;


                /// <summary>
                /// Action, which would be invoked after recieving every portion of bytes from server for current request.
                /// </summary>
                [JsonIgnore]
                public Action<DownloadProgressChangedEventArgs> OnDownloadProgressChangedAction { get; set; }
            }

            public class ResponseEntity
            {
                /// <summary>
                /// Here you may get HTTP response code, if no exception was thrown during the request.
                /// </summary>
                [JsonIgnore]
                public HttpStatusCode HttpCode { get; set; }

                /// <summary>
                /// Here you may get exception, if it was thrown during the request.
                /// </summary>
                public Exception Exception { get; set; }

                /// <summary>
                /// It's just custom field, usually server or API return's some error code for each request. You may redefine that field in your some inherit BaseRequestEntity/Model and add [JsonIgnore] attribute to hide that field OR use [JsonProperty("NEW_NAME")] to rename that field to your's server responses.
                /// </summary>
                public int ErrorCode { get; set; }

                /// <summary>
                /// It's just custom field, usually server or API return's some error message for each request. You may redefine that field in your some inherit BaseRequestEntity/Model and add [JsonIgnore] attribute to hide that field OR use [JsonProperty("NEW_NAME")] to rename that field to your's server responses.
                /// </summary>
                public string ErrorMessage { get; set; }

                /// <summary>
                /// Response body in a string format.May be usefull, if you need to download HTML page or simple text/string. Also you may set string as a return\generic parameter type for your request method, and response would be converted automatically to string.
                /// </summary>
                public string ResponseText { get; set; }

                /// <summary>
                /// Dynamic object, that represents attempt to convert response as a json to dynamic object. Added because we can.
                /// </summary>
                public dynamic ResponseDynamic { get; set; }

                /// <summary>
                /// Response body in it's original form. May be usefull, if you need to download file or get response body without any processing.
                /// </summary>
                public byte[] ResponseBody { get; set; }
            }
        }

        #region Props and fields

        /// <summary>
        /// The response from server
        /// </summary>
        public static ResponseEntity LastResponse { get; private set; }

        private static string _baseUrl;

        private static string BaseUrl
        {
            get => _baseUrl ?? throw new ArgumentException("Base URL is not set!");
            set => _baseUrl = string.IsNullOrWhiteSpace(value) ? null : value.TrimEnd('/') + '/';
        }

        protected static List<HeaderAttribute> GlobalHeaders { get; set; } = new List<HeaderAttribute>();

        /// <summary>
        /// Init library
        /// </summary>
        /// <param name="baseUrl">Host name for requests</param>
        /// <param name="headers">Collection of headers for requests</param>
        public static void Init(string baseUrl, List<HeaderAttribute> headers = null)
        {
            BaseUrl = baseUrl ?? _baseUrl;
            GlobalHeaders = headers ?? GlobalHeaders ?? new List<HeaderAttribute>();
        }

        /// <summary>
        /// The maximum number of bytes to read.
        /// </summary>
        private const int BUFFER_SIZE = 81920;

        /// <summary>
        /// Event, which would be invoked after recieving every portion of bytes from server for current request.
        /// </summary>
        public static event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;

        #endregion


        /// <summary>
        /// Provides data for the DownloadProgressChanged event of a HTTPWebResponse.
        /// </summary>
        public class DownloadProgressChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the length of the content returned by the request.
            /// </summary>
            public long ResponseBodyLength { get; set; }


            /// <summary>
            /// Gets the number of bytes received.
            /// </summary>
            public long RecievedBytesCount { get; set; }


            /// <summary>
            /// Recieved data
            /// </summary>
            public byte[] RecievedBytes { get; set; }


            /// <summary>
            /// Gets the total number of bytes in a HTTPWebResponse data download operation.
            /// </summary>
            public long TotalRecievedBytesCount { get; set; }
        }
    }
}