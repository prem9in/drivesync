#r "System.Web"
#r "Newtonsoft.Json"

#load "./bufferedstream.csx"

using System;
using System.IO;
using System.Web;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Runtime.Serialization;

public static class Http
{
    private static HttpClient client;

    static Http()
    {
        client = new HttpClient(GetHandler());
        client.Timeout = TimeSpan.FromHours(1);
    }    

    public static async Task<Stream> MakeRequestForFile(string url, HttpMethod method, Dictionary<string, string> headers, string postData, string postMediaType, long size)
    {
        var fileReq = (HttpWebRequest)WebRequest.Create(url);

        if (headers != null && headers.Any())
        {
            foreach (var header in headers)
            {
                fileReq.Headers.Add(header.Key, header.Value);
            }
        }

        var fileResp = (HttpWebResponse)fileReq.GetResponse();
        if (fileResp.StatusCode == HttpStatusCode.OK)
        {
            var stream = fileResp.GetResponseStream();
            var bs = new BufferedStream();
            bs.ReadFrom(stream, size+100);            
            bs.Position = 0;
            return bs;
        }
        else
        {
            return null;
        }
    }

    public static async Task<Tuple<TResponse, TError>> MakeRequest<TResponse, TError>(string url, HttpMethod method, Dictionary<string, string> headers, string postData, string postMediaType)
    {
        var response = await Send(url, method, headers, postData, postMediaType, System.Net.Http.HttpCompletionOption.ResponseContentRead);
        var result = await response.Content.ReadAsStringAsync();
        return ParseResponse<TResponse, TError>(result);        
    }

    private static async Task<HttpResponseMessage> Send(string url, HttpMethod method, Dictionary<string, string> headers, string postData, string postMediaType, System.Net.Http.HttpCompletionOption completionOption)
    {                 
            var request = new HttpRequestMessage() { RequestUri = new Uri(url), Method = method };
            if (headers != null && headers.Any())
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            if (method == HttpMethod.Post)
            {
                request.Content = new StringContent(postData, Encoding.UTF8, postMediaType);
            }

            var tokenSource = new CancellationTokenSource(60 * 1000 * 60); // one hour
            var response = await client.SendAsync(request, completionOption, tokenSource.Token);
            return response;                    
    }

    private static Tuple<TResponse, TError> ParseResponse<TResponse, TError>(string message)
    {
        try
        {
            var result = JsonConvert.DeserializeObject<TResponse>(message);
            return Tuple.Create<TResponse, TError>(result, default(TError));
        }
        catch
        {
            var error = JsonConvert.DeserializeObject<TError>(message);
            return Tuple.Create<TResponse, TError>(default(TResponse), error);
        }
    }

    private static HttpClientHandler GetHandler()
    {
        var handler = new HttpClientHandler()
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10
        };
        return handler;
    }
}
