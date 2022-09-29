using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace custom_idp.proxy.Controllers
{
    public class HttpRequestHelper
    {

        public static async Task<HttpRequestMessage> CreateRequestHttpMessageAsync(HttpRequest Request, string IdpEndpoint)
        {
            // The request message
            var requestMessage = new HttpRequestMessage();

            // Get the target URL
            Uri targetUri = GetTargetUrl(Request, IdpEndpoint);

            // Set the URL
            requestMessage.RequestUri = targetUri;
            requestMessage.Headers.Host = targetUri.Host;

            // Set the HTTP method
            requestMessage.Method = GetMethod(Request.Method);

            // Copy the request HTTP headers
            foreach (var header in Request.Headers)
            {
                if (header.Key == "Authorization" || header.Key == "User-Agent")
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            // Copy the request HTTP body (if any)
            if (!HttpMethods.IsGet(Request.Method) &&
              !HttpMethods.IsHead(Request.Method) &&
              !HttpMethods.IsDelete(Request.Method) &&
              !HttpMethods.IsTrace(Request.Method))
            {
                // IMPORTANT: this code supports only application/x-www-form-urlencoded
                var dict = new Dictionary<string, string>();
                foreach (var item in Request.Form)
                {
                    dict.Add(item.Key, item.Value);
                }

                requestMessage.Content = new FormUrlEncodedContent(dict);
            }

            return requestMessage;
        }

        public static Uri GetTargetUrl(HttpRequest Request, string IdpEndpoint)
        {
            string decodedIdpEndpoint = string.Empty;
            try
            {
                byte[] data = Convert.FromBase64String(Uri.UnescapeDataString(IdpEndpoint.Replace("_", "/")));
                decodedIdpEndpoint = Encoding.UTF8.GetString(data);
            }
            catch (System.Exception)
            {
                throw new Exception($"Error decoding the base64 URL: '{IdpEndpoint}'");
            }

            string queryStrings = Request.QueryString.ToString();
            if (decodedIdpEndpoint.Contains("?"))
            {
                queryStrings = queryStrings.Replace("?", "&");
            }

            Uri targetUri = new Uri(decodedIdpEndpoint + queryStrings);

            return targetUri;
        }

        private static HttpMethod GetMethod(string method)
        {
            if (HttpMethods.IsDelete(method)) return HttpMethod.Delete;
            if (HttpMethods.IsGet(method)) return HttpMethod.Get;
            if (HttpMethods.IsHead(method)) return HttpMethod.Head;
            if (HttpMethods.IsOptions(method)) return HttpMethod.Options;
            if (HttpMethods.IsPost(method)) return HttpMethod.Post;
            if (HttpMethods.IsPut(method)) return HttpMethod.Put;
            if (HttpMethods.IsTrace(method)) return HttpMethod.Trace;
            return new HttpMethod(method);
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes).Replace("/", "_");
        }
    }

}// This code is based on https://github.com/aspnet/Mvc/blob/release/2.2/src/Microsoft.AspNetCore.Mvc.WebApiCompatShim/Formatters/HttpResponseMessageOutputFormatter.cs