#r "System.Web"
#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

#load "./httpclient.csx"
#load "./config.csx"
#load "./../models/oauthcoderesponse.csx"
#load "./../models/oautherrorresponse.csx"
#load "./../models/authinfo.csx"
#load "./../models/oauthclaims.csx"
#load "./../models/runtime.csx"

using System;
using System.Text;
using System.Web;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Linq;
using System.Collections.Specialized;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;

public static class OAuth
{
    private static AuthInfo _userAuth = null;
    private static object lockObj = new object();
    public static string GetAccessoken(Runtime runtime)
    {
        if (_userAuth == null)
        {
            lock (lockObj)
            {
                if (_userAuth == null)
                {
                    _userAuth = runtime.AuthConnect.Where(c => c.PartitionKey == "AuthInfo" && c.RowKey == AppConfiguration.OAuthDefaultUser).FirstOrDefault();
                }
            }  
        }

        if (_userAuth != null)
        {
            if (_userAuth.RefreshBefore < DateTime.UtcNow)
            {
                //// we need to refresh 
                lock (lockObj)
                {
                    if (_userAuth.RefreshBefore < DateTime.UtcNow)
                    {
                        var postData = GetRefreshTokenPost(_userAuth.RefreshToken, runtime.Log);
                        var tokenTask = GetToken(postData, runtime.Log);
                        tokenTask.Wait();
                        if (tokenTask.Result.Item1 == null)
                        {
                            return string.Empty;
                        }
                        else
                        {
                            var updateTask = ProcessAuthAndSave(runtime, tokenTask.Result.Item1);
                            updateTask.Wait();
                            _userAuth = updateTask.Result;
                        }
                    }
                }
            }

            return _userAuth.AccessToken;
        }
        
        return string.Empty;
    }

    public static HttpResponseMessage RedirectToOAuth(Runtime runtime)
    {
        var response = runtime.Request.CreateResponse(HttpStatusCode.Redirect);
        response.Headers.Location = GetOAuthUrl(runtime.Log);
        return response;
    }

    public static async Task<HttpResponseMessage> CreateConnection(Runtime runtime)
    {
        var content = runtime.Request.Content;
        string postMessage = await content.ReadAsStringAsync();
        var codeResult = ParseCodeMessage(postMessage);
        if (codeResult.Item1 == null)
        {
            return runtime.Request.CreateResponse(HttpStatusCode.BadRequest, codeResult.Item2, new JsonMediaTypeFormatter());
        }
        else
        {
            var codeResponse = codeResult.Item1;
            var postData = GetTokenPost(codeResponse.Code, runtime.Log);
            return await AcquireToken(runtime, postData);
        }
    }

    private static async Task<HttpResponseMessage> AcquireToken(Runtime runtime, string postData)
    {
        var tokenResult = await GetToken(postData, runtime.Log);        
        if (tokenResult.Item1 == null)
        {
            return runtime.Request.CreateResponse(HttpStatusCode.BadRequest, tokenResult.Item2, new JsonMediaTypeFormatter());
        }
        else
        {
            var authInfo = await ProcessAuthAndSave(runtime, tokenResult.Item1);         
            return runtime.Request.CreateResponse(HttpStatusCode.OK, authInfo, new JsonMediaTypeFormatter());
        }
    }

    private static async Task<AuthInfo> ProcessAuthAndSave(Runtime runtime, AuthInfo authInfo)
    {        
        authInfo.UserId = ReadUserFromJwt(authInfo.IdToken);
        if (string.IsNullOrWhiteSpace(authInfo.UserId))
        {
            authInfo.UserId = AppConfiguration.OAuthDefaultUser;
        }

        var secondsToExpiry = Int32.Parse(authInfo.ExpiresIn) - 60 * 10; //// 10 minutes
        authInfo.RefreshBefore = DateTime.UtcNow.AddSeconds(secondsToExpiry);
        authInfo.Timestamp = DateTime.UtcNow;
        authInfo.PartitionKey = "AuthInfo";
        authInfo.RowKey = authInfo.UserId;
        authInfo.ETag = "*";
        var operation = TableOperation.InsertOrReplace(authInfo);
        await runtime.OnedriveConnect.ExecuteAsync(operation);
        return authInfo;
    }


    private static string ReadUserFromJwt(string idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return string.Empty;
        }

        var jwtParts = idToken.Split(new[] { '.' });
        if (jwtParts.Length == 3)
        {
            var header = jwtParts[0];
            var payload = jwtParts[1];
            var signature = jwtParts[2];
            payload = payload.Replace('-', '+'); // 62nd char of encoding
            payload = payload.Replace('_', '/'); // 63rd char of encoding
            switch (payload.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: payload += "=="; break; // Two pad chars
                case 3: payload += "="; break; // One pad char
                default:
                    throw new System.Exception("llegal base64url string!");
            }
            byte[] buffer = Convert.FromBase64String(payload);
            var claims = Encoding.UTF8.GetString(buffer);
            var authClaims = JsonConvert.DeserializeObject<OAuthClaims>(claims);
            return authClaims.PrincipalName;
        }

        return string.Empty;
    }

    private static async Task<Tuple<AuthInfo, OAuthErrorResponse>> GetToken(string data, TraceWriter log)
    {
        return await Http.MakeRequest<AuthInfo, OAuthErrorResponse>(GetOAuthTokenUrl(log), HttpMethod.Post, null, data, "application/x-www-form-urlencoded");        
    }


    private static string GetTokenPost(string code, TraceWriter log)
    {
        var data = string.Format(AppConfiguration.OAuth_TokenPostFormat,
                           AppConfiguration.OAuth_ClientId,
                           AppConfiguration.OAuth_Scope,
                           AppConfiguration.OAuth_ClientSecret,
                           AppConfiguration.OAuth_GrantType,
                           code,
                           AppConfiguration.OAuth_RedirectUrl);
        return data;
    }

    private static string GetRefreshTokenPost(string refreshToken, TraceWriter log)
    {
        var data = string.Format(AppConfiguration.OAuth_RefreshTokenPostFormat,
                           AppConfiguration.OAuth_ClientId,
                           AppConfiguration.OAuth_Scope,
                           AppConfiguration.OAuth_ClientSecret,
                           AppConfiguration.OAuth_GrantTypeRefreshToken,
                           refreshToken,
                           AppConfiguration.OAuth_RedirectUrl);
        return data;
    }

    private static string GetOAuthTokenUrl(TraceWriter log)
    {
        return string.Format(AppConfiguration.OAuth_TokenFormat, AppConfiguration.OAuth_BaseUri);
    }

    private static Uri GetOAuthUrl(TraceWriter log)
    {
        var state = Guid.NewGuid();
        var endpoint = string.Format(AppConfiguration.OAuth_AuthorizeFormat,
                            AppConfiguration.OAuth_BaseUri,
                            AppConfiguration.OAuth_ClientId,
                            AppConfiguration.OAuth_ResponseType,
                            AppConfiguration.OAuth_RedirectUrl,
                            AppConfiguration.OAuth_ResponseMode,
                            AppConfiguration.OAuth_DomainHint,
                            AppConfiguration.OAuth_Scope,
                            Guid.NewGuid());
        log.Info("Outh URL generated with state : " + state.ToString("D"));
        return new Uri(endpoint);
    }

    private static Tuple<OAuthCodeResponse, OAuthErrorResponse> ParseCodeMessage(string message)
    {
        var dataCollection = HttpUtility.ParseQueryString(message);
        if (string.IsNullOrWhiteSpace(dataCollection["error"]))
        {
            return Tuple.Create<OAuthCodeResponse, OAuthErrorResponse>(new OAuthCodeResponse(dataCollection), null);
        }
        else
        {
            return Tuple.Create<OAuthCodeResponse, OAuthErrorResponse>(null, new OAuthErrorResponse(dataCollection));
        }
    }
   
}