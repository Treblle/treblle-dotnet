using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.IO;

namespace Treblle
{
    public class TreblleActionFilterAttribute : ActionFilterAttribute
    {
        private static TreblleConfig config { get; set; }
        private string RequestContentType { get; set; }
        private string ResponseContentType { get; set; }

        public TreblleActionFilterAttribute()
        {

        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            actionContext.Request.Properties.Add("ActionStart", DateTime.Now);
            base.OnActionExecuting(actionContext);
        }

        private JToken GetRequestContent(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext?.Request?.Content != null)
            {
                RequestContentType = !string.IsNullOrEmpty(actionExecutedContext.Request.Content.Headers?.ContentType?.MediaType)
                                    ? actionExecutedContext.Request.Content.Headers.ContentType.MediaType
                                    : "";

                using (var stream = actionExecutedContext?.Request?.Content?.ReadAsStreamAsync().Result)
                {
                    if (stream?.CanSeek == true)
                    {
                        stream.Position = 0;
                    }
                    return ReadBody(actionExecutedContext?.Request?.Content?.ReadAsStringAsync().Result, RequestContentType);
                }
            }
            else
            {
                return null;
            }
        }

        private JToken GetResponseContent(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext?.Response?.Content != null)
            {
                ResponseContentType = !string.IsNullOrEmpty(actionExecutedContext.Response.Content.Headers?.ContentType?.MediaType)
                                    ? actionExecutedContext.Response.Content.Headers.ContentType.MediaType
                                    : "";

                //we only want to send json to Treblle
                if (ResponseContentType != "application/json")
                    return null;

                // The next line, that contains a using and because of that automatically calls Dispose method,
                // would result in response 500 Internal Server Error from an actual web service that is logged (response 500 didn't appear in Treblle).
                //using (var stream = actionExecutedContext?.Response?.Content?.ReadAsStreamAsync().Result)

                // Copy response into a second stream = helper stream, so we can read a response and not dispose an original stream.
                Stream responseCopyStream = new MemoryStream();
                actionExecutedContext?.Response?.Content.CopyToAsync(responseCopyStream);

                // Read a response from the helper stream.
                using (responseCopyStream)
                {
                    if (responseCopyStream?.CanSeek == true)
                    {
                        responseCopyStream.Position = 0;
                    }
                    byte[] byteArrayResponse = new byte[responseCopyStream.Length];
                    int bytesRead = responseCopyStream.Read(byteArrayResponse, 0, (int)responseCopyStream.Length);

                    if (bytesRead != responseCopyStream.Length)
                        Debug.WriteLine("Error reading response in method GetResponseContent().");

                    string strResponse = Encoding.UTF8.GetString(byteArrayResponse);
                    return ReadBody(strResponse, ResponseContentType);
                }
            }
            else
            {
                return null;
            }
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            // Make a deep copy of server variables, using copy constructor,
            // so that we have a copy that can be used in async method independently  of an original server variables.
            var serverVariablesDeepCopy = new NameValueCollection(System.Web.HttpContext.Current.Request.ServerVariables);

            base.OnActionExecuted(actionExecutedContext);
            actionExecutedContext.Request.Properties.Add("ActionEnd", DateTime.Now);

            // Copy content of the request and content of the response,
            // so that we have copies that can be used in async method independently  of an original server variables.
            JToken requestContent = GetRequestContent(actionExecutedContext);
            JToken responseContent = GetResponseContent(actionExecutedContext);

            //if it's not null that means that it was a json call which was successfuly read and can be sent further
            if (requestContent != null && responseContent != null)
                Task.Run(() => MakeTreblleCall(actionExecutedContext, serverVariablesDeepCopy, requestContent, responseContent));
        }

        private async Task MakeTreblleCall(HttpActionExecutedContext actionExecutedContext, NameValueCollection serverVariables, JToken requestContent, JToken responseContent)
        {
            try
            {
                if (config == null)
                    config = new TreblleConfig();

                var request = new TreblleCall();
                request.ApiKey = config.TreblleApiKey;
                request.ProjectId = config.TreblleProjectId;
                request.Sdk = ".NET";
                request.Version = "0.1";

                request.TreblleData.Server.Signature = serverVariables != null ? serverVariables["CERT_SERVER_ISSUER"] : "";
                request.TreblleData.Server.Protocol = serverVariables != null ? serverVariables["SERVER_PROTOCOL"] : "";
                request.TreblleData.Server.Encoding = serverVariables != null ? serverVariables["HTTP_ACCEPT_ENCODING"] : "";

                #region [REQUEST]
                if (actionExecutedContext.Request != null)
                {
                    request.TreblleData.TreblleRequest.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    request.TreblleData.TreblleRequest.IP = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.MapToIPv4().ToString();
                    request.TreblleData.TreblleRequest.URL = actionExecutedContext.Request.RequestUri.AbsoluteUri;
                    request.TreblleData.TreblleRequest.Method = actionExecutedContext.Request.Method.Method;

                    if (actionExecutedContext.Request.Headers != null)
                    {
                        foreach (var header in actionExecutedContext.Request.Headers)
                        {
                            if (!string.IsNullOrEmpty(header.Key) && header.Value?.Count() > 0)
                                request.TreblleData.TreblleRequest.Headers.Add(header.Key, string.Join(",", header.Value));
                        }
                    }

                    if (actionExecutedContext.Request.Content.Headers != null)
                    {
                        foreach (var header in actionExecutedContext.Request.Content.Headers)
                        {
                            if (!string.IsNullOrEmpty(header.Key) && header.Value?.Count() > 0 && !request.TreblleData.TreblleRequest.Headers.ContainsKey(header.Key))
                                request.TreblleData.TreblleRequest.Headers.Add(header.Key, string.Join(",", header.Value));
                        }
                    }

                    request.TreblleData.TreblleRequest.UserAgent = request.TreblleData.TreblleRequest.Headers.ContainsKey("User-Agent") ? request.TreblleData.TreblleRequest.Headers["User-Agent"] : "";

                    request.TreblleData.TreblleRequest.Body = requestContent;
                }
                #endregion

                #region [RESPONSE]
                if (actionExecutedContext.Response != null)
                {
                    request.TreblleData.TreblleResponse.Code = (int)actionExecutedContext.Response.StatusCode;

                    if (actionExecutedContext.Response.Headers != null)
                    {
                        foreach (var header in actionExecutedContext.Response.Headers)
                        {
                            if (!string.IsNullOrEmpty(header.Key) && header.Value?.Count() > 0)
                                request.TreblleData.TreblleResponse.Headers.Add(header.Key, string.Join(",", header.Value));
                        }
                    }

                    if (actionExecutedContext.Response.Content.Headers != null)
                    {
                        foreach (var header in actionExecutedContext.Response.Content.Headers)
                        {
                            if (!string.IsNullOrEmpty(header.Key) && header.Value?.Count() > 0 && !request.TreblleData.TreblleResponse.Headers.ContainsKey(header.Key))
                                request.TreblleData.TreblleResponse.Headers.Add(header.Key, string.Join(",", header.Value));
                        }
                    }

                    request.TreblleData.TreblleResponse.Loadtime = (int)((DateTime)actionExecutedContext.Request.Properties["ActionEnd"] - (DateTime)actionExecutedContext.Request.Properties["ActionStart"]).TotalMilliseconds;

                    request.TreblleData.TreblleResponse.Body = responseContent;

                    request.TreblleData.TreblleResponse.Size = CalculateResponseSize(request.TreblleData.TreblleResponse);
                }
                #endregion

                #region [ERROR]
                if (actionExecutedContext.Exception != null)
                {
                    var errors = string.Format(@"{{
                        'Message':'{0}',
                        'InnerException':'{1}',
                        'Source':'{2}',
                        'StackTrace':'{3}',
                        'TargetSite':'{4}'
                    }}",
                        actionExecutedContext.Exception.Message,
                        actionExecutedContext.Exception.InnerException != null ? actionExecutedContext.Exception.InnerException.ToString() : "",
                        actionExecutedContext.Exception.Source,
                        actionExecutedContext.Exception.StackTrace,
                        actionExecutedContext.Exception.TargetSite != null ? actionExecutedContext.Exception.TargetSite.ToString() : ""
                    );

                    //we replace \ because of the path in the stack trace so the parser doesn't break
                    request.TreblleData.Errors.Add(JObject.Parse(errors.Replace("\\", "\\\\")));
                }
                #endregion

                #region [SEND REQUEST]
                var json = string.Format(
                    "{{\"body\":{0}}}",
                    JsonConvert.SerializeObject(request, Formatting.Indented)
                );
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("https://rocknrolla.treblle.com");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); //ACCEPT header
                client.DefaultRequestHeaders.Add("x-api-key", request.ApiKey);

                //var response = await client.PostAsync("https://rocknrolla.treblle.com", data);
                await client.PostAsync("https://rocknrolla.treblle.com", data);
                //var resp = response.Content.ReadAsStringAsync();
                //var res = resp.Result;
                #endregion
            }
            catch (Exception ex)
            {
                //TO-DO Write to log
            }
        }

        public JToken ReadBody(string result, string contentType)
        {
            //if it is json check if it is an object or array
            return contentType.Contains("application/json")
                ? JToken.Parse(result)
                : string.IsNullOrEmpty(result) ? new JObject() : JToken.Parse($"{{body: '{result}'}}");
        }

        //TO-DO - improve calculation of response size
        private int CalculateResponseSize(TreblleResponse response)
        {
            int size = response.Body != null ? response.Body.ToString().Length : 0;
            //add size of name fields (code, headers, size, load_time, body) with characters : and ,
            size += 47;
            size += response.Loadtime.ToString().Length;
            size += response.Code.ToString().Length;

            if (response.Headers?.Count > 0)
            {
                foreach (var h in response.Headers)
                {
                    size += (h.Key.Length + h.Value.Length + 6/*length of character ",:*/);
                }

                //last response header doesn't have a comma so reduce size by 1
                size -= 1;
            }

            return size;
        }
    }
}
