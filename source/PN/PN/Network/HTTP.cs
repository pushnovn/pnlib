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


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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
        protected static dynamic Base(RequestEntity requestModel = null) =>
            CreatePrivateBase(requestModel, GetMethodInfo());


        /// <summary>
        /// All downloading data will be stored at RAM. If you want change this, set flushBuffer value to true, for clearing memory after downloading part of data. 
        /// </summary>
        /// <param name="customAction">Is action will be invoked every time when new data will arrived. Use it when you trying to download 2 or more files in a row</param>
        /// <returns>Method will return object that you specified as generic type</returns>
        public static TResponse Request<TResponse>(string url, RequestEntity requestEntity = null, params object[] settings)
        {
            var headers = settings.OfType<HeaderAttribute>()?.ToList();
            settings.OfType<IEnumerable<HeaderAttribute>>()?.ToList()?.ForEach(l => headers.AddRange(l));

            var methodInfo = new ReflMethodInfo()
            {
                MethodPath = url,
                BaseReturnType = typeof(TResponse),
                IsGenericType = typeof(TResponse).IsGenericType,
                ReturnType = typeof(TResponse).IsGenericType
                    ? typeof(TResponse).GetGenericArguments()[0]
                    : typeof(TResponse),
                RequestType = settings.OfType<RequestTypes>().LastOrDefault(),
                ContentType = settings.OfType<ContentTypes>().LastOrDefault(),
                IgnoreGlobalHeaders = settings.OfType<IgnoreGlobalHeadersAttribute>()?.Count() > 0,
                HeaderAttributes = headers,
            };

            return (TResponse)CreatePrivateBase(requestEntity, methodInfo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object CreatePrivateBase(RequestEntity requestModel, ReflMethodInfo methodInfo,
            Action<ProgressChangedEventArgs> customAction = null)
        {
            var method = typeof(HTTP).GetMethod(nameof(BaseAsyncPrivate), BindingFlags.NonPublic | BindingFlags.Static);
            var generic = method?.MakeGenericMethod(methodInfo.ReturnType);
            var task = generic?.Invoke(null, new object[] { requestModel, customAction });

            return methodInfo.IsGenericType
                ? task
                : task.GetType().GetProperty(nameof(Task<dynamic>.Result)).GetValue(task, null);
        }

        private static async Task<T> BaseAsyncPrivate<T>(RequestEntity requestModel, ReflMethodInfo methodInfo)
        {
            try
            {
                #region URL and request type

                requestModel = requestModel ?? new RequestEntity();
                var requestUri =
                    new Uri(Utils.Utils.Internal.ProcessComplexString(methodInfo.MethodFullUrl, requestModel));
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
                request.Method = methodInfo.RequestType.ToString();
                request.ContentType = ContentTypeToString(methodInfo.ContentType);
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

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        var responseBody = await GetResponseBodyWithProgressAsync(requestModel, response.ContentLength, stream);
                        var responseJson = Encoding.UTF8.GetString(responseBody);
                        LastResponse = new ResponseEntity() { ResponseBody = responseBody, ResponseText = responseJson };

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
                        Utils.Utils.Internal.TrySetValue(ref instance, (int)response.StatusCode, nameof(ResponseEntity.HttpCode));

                        return (T)instance;
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                LastResponse = new ResponseEntity() { Exception = ex };
                var instance = Utils.Utils.Internal.CreateDefaultObject(methodInfo.ReturnType);
                return (T)Utils.Utils.Internal.TrySetValue(ref instance, ex, nameof(ResponseEntity.Exception));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ReflMethodInfo GetMethodInfo()
        {
            var method = new StackTrace().GetFrame(2).GetMethod();

            var baseResponseModelType = (method as MethodInfo)?.ReturnType;
            var isGenericType = baseResponseModelType.IsGenericType;
            var responseModelType =
                isGenericType ? baseResponseModelType.GetGenericArguments()[0] : baseResponseModelType;

            var url = method.GetCustomAttributes()?.OfType<UrlAttribute>()?.FirstOrDefault();
            var requestType = method.GetCustomAttributes()?.OfType<RequestTypeAttribute>()?.FirstOrDefault();
            var contentType = method.GetCustomAttributes()?.OfType<ContentTypeAttribute>()?.FirstOrDefault();
            var ignoreGlobalHeaders =
                method.GetCustomAttributes()?.OfType<IgnoreGlobalHeadersAttribute>()?.FirstOrDefault();
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
                ReturnType = responseModelType,
                BaseReturnType = baseResponseModelType,
                IsGenericType = isGenericType,
                MethodPath = temp_uri.TrimEnd('/'),
                MethodPartialUrl = url?.Url ?? method.Name,
                RequestType = requestType?.RequestType ?? RequestTypes.GET,
                ContentType = contentType?.ContentType ?? ContentTypes.JSON,
                IgnoreGlobalHeaders = ignoreGlobalHeaders != null,
                HeaderAttributes = headers,
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

                var args = new ProgressChangedEventArgs
                {
                    MaxBytes = responseContentLength,
                    Recieved = bytesRead,
                    TotalRecieved = totalRecieved,
                    RecievedData = bytes
                };

                requestModel.CustomAction?.Invoke(args);
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
            internal string MethodPath { get; set; }
            internal Type ReturnType { get; set; }
            internal Type BaseReturnType { get; set; }
            internal bool IsGenericType { get; set; }

            internal string MethodPartialUrl { get; set; }

            internal string MethodFullUrl => (MethodPath ?? "") +
                                             (!string.IsNullOrWhiteSpace(MethodPath) &&
                                              !string.IsNullOrWhiteSpace(MethodPartialUrl)
                                                 ? "/"
                                                 : "") + (MethodPartialUrl?.Trim('/') ?? "");

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

            public UrlAttribute(string url)
            {
                Url = url;
            }
        }

        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        protected class RequestTypeAttribute : Attribute
        {
            public readonly RequestTypes RequestType;

            public RequestTypeAttribute(RequestTypes requestType)
            {
                RequestType = requestType;
            }
        }

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

        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        public class IgnoreGlobalHeadersAttribute : Attribute
        {
        }

        #endregion

        public enum RequestTypes
        {
            GET,
            POST,
            PUT,
            DELETE
        }

        public enum ContentTypes
        {
            JSON
        }

        public class Entities
        {
            public class RequestEntity
            {
                [JsonIgnore] public List<HeaderAttribute> Headers { get; set; }

                [JsonIgnore] public byte[] Body { get; set; }

                [JsonIgnore] public int? Timeout { get; set; }
                /// <summary>
                /// Flush the buffer after receiving, if true, buffer will be flushed every time after invoking DownloadProgressChanged event</param>
                /// </summary>
                [JsonIgnore] public bool FlushBuffer { get; set; } = false;
                [JsonIgnore] public Action<ProgressChangedEventArgs> CustomAction { get; set; }
            }

            public class ResponseEntity
            {
                [JsonIgnore] public int HttpCode { get; set; }

                public Exception Exception { get; set; }
                public int ErrorCode { get; set; }
                public string ErrorMessage { get; set; }

                public string ResponseText { get; set; }
                public dynamic ResponseDynamic { get; set; }
                public byte[] ResponseBody { get; set; }
            }
        }

        #region Props and fields

        public static ResponseEntity LastResponse { get; private set; }

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


        private const int BUFFER_SIZE = 81920;

        public static event EventHandler<ProgressChangedEventArgs> DownloadProgressChanged;

        public class ProgressChangedEventArgs : EventArgs
        {
            public long MaxBytes { get; set; }
            public long Recieved { get; set; }
            public long TotalRecieved { get; set; }
            public byte[] RecievedData { get; set; }
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member