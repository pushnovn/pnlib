using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using static PN.Network.HTTP.Entities;

namespace PN.Network
{
    public abstract class HTTP
    {
        #region Base request logic

        protected static dynamic Base(RequestEntity requestModel)
        {
            var methodInfo = GetMethodInfo();

            return TryExecuteAction(methodInfo.ReturnType, () =>
            {
                var requestUri = new Uri(
                    $"{BaseApiUrl}" +
                    $"{methodInfo.ClassName.ToLower()}" +
                    $"{(string.IsNullOrWhiteSpace(methodInfo.ApiMethodName) ? string.Empty : $"/{ProcessComplexUrl(methodInfo.ApiMethodName, requestModel)}")}");

                var response = Request(methodInfo.ReturnType, methodInfo.ApiMethodType, requestModel, requestUri);

                return Convert.ChangeType(response, methodInfo.ReturnType);
            });
        }

        private static object Request(
            Type responseModelType, 
            RequestTypes requestType, 
            RequestEntity body = null, 
            Uri requestUri = null)
        {
            return TryExecuteAction(responseModelType, () =>
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
                request.Method = requestType.ToString();

                //if (!string.IsNullOrEmpty(token))
                //    request.Headers["X-Session-Token"] = token;

                var requestJson = JsonConvert.SerializeObject(body);
                if (!requestJson.Equals("{}") && !requestJson.Equals("null"))
                {
                    byte[] requestBody = Encoding.UTF8.GetBytes(requestJson);

                    request.ContentLength = requestBody.Length;
                    request.ContentType = "application/json";

                    using (Stream stream = request.GetRequestStream())
                        stream.Write(requestBody, 0, requestBody.Length);
                }

                return ExecuteRequest(request, responseModelType);
            });
        }

        private static object ExecuteRequest(HttpWebRequest request, Type responseModelType)
        {
            return TryExecuteAction(responseModelType, () =>
            {
                using (WebResponse response = request.GetResponse())
                {
                    var responseJson = string.Empty;
                    using (Stream responseStream = response.GetResponseStream())
                        responseJson = new StreamReader(responseStream).ReadToEnd();
                    return JsonConvert.DeserializeObject(responseJson, responseModelType);
                }
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


        private static string _baseApiUrl;
        protected static string BaseApiUrl
        {
            get => _baseApiUrl ?? throw new ArgumentException("Base API url is not set!");
            set => _baseApiUrl = value?.Trim() == string.Empty ? null : value;
        }


        private static ReflMethodInfo GetMethodInfo()
        {
            StackTrace st = new StackTrace();
            StackFrame[] fr = st.GetFrames();

            if (fr != null)
            {
                var method = fr[2].GetMethod();
                var item = method.GetCustomAttributes()?.OfType<MethodAttribute>()?.First();

                return new ReflMethodInfo()
                {
                    ReturnType = method is MethodInfo ? (method as MethodInfo).ReturnType : null,
                    Name = method.Name,
                    ClassName = method.ReflectedType.Name,
                    ApiMethodName = item?.Name,
                    ApiMethodType = item.Type,
                    IsPaymentApi = item.IsPayment,
                };
            }

            return null;
        }

        private static string ProcessComplexUrl(string urlTemplate, RequestEntity model)
        {
            MatchCollection matches = Regex.Matches(urlTemplate, @"\{[\w]+\}");

            var str = urlTemplate.Substring(0, matches.Count > 0 ? matches[0].Index : urlTemplate.Length);

            for (int i = 0; i < matches.Count; i++)
            {
                Match m = matches[i];

                str += model.GetType().GetProperty(m.Value.Substring(1, m.Length - 2)).GetValue(model) as String ?? m.Value;

                var indexOfLastMatchChar = m.Index + m.Length;

                var nextClearPartLength = -indexOfLastMatchChar + (i + 1 < matches.Count ? matches[i + 1].Index : urlTemplate.Length);

                str += urlTemplate.Substring(indexOfLastMatchChar, nextClearPartLength);
            }

            return str;
        }

        private class ReflMethodInfo
        {
            public string Name { get; set; }
            public string ClassName { get; set; }
            public Type ReturnType { get; set; }
            public string ApiMethodName { get; set; }
            public RequestTypes ApiMethodType { get; set; }
            public bool IsPaymentApi { get; set; }
        }

        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
        protected class MethodAttribute : Attribute
        {
            public readonly string Name;
            public readonly RequestTypes Type;
            public readonly bool IsPayment;

            public MethodAttribute(string name, RequestTypes type = RequestTypes.POST, bool isPayment = false)
            {
                Name = name;
                Type = type;
                IsPayment = isPayment;
            }
        }

        protected enum RequestTypes { GET, POST }

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

        #endregion
    }
}