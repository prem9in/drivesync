#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using Microsoft.WindowsAzure.Storage.Table;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;

public class AuthInfo : TableEntity
{
    [JsonProperty(PropertyName = "userid")]
    public string UserId { get; set; }

    [JsonProperty(PropertyName = "access_token")]
    public string AccessToken { get; set; }

    [JsonProperty(PropertyName = "token_type")]
    public string TokenType { get; set; }

    [JsonProperty(PropertyName = "expires_in")]
    public string ExpiresIn { get; set; }

    [JsonProperty(PropertyName = "expires_on")]
    public string ExpiresOn { get; set; }

    [JsonProperty(PropertyName = "refresh_before")]
    public DateTime RefreshBefore { get; set; }

    [JsonProperty(PropertyName = "resource")]
    public string Resource { get; set; }

    [JsonProperty(PropertyName = "refresh_token")]
    public string RefreshToken { get; set; }

    [JsonProperty(PropertyName = "scope")]
    public string Scope { get; set; }

    [JsonProperty(PropertyName = "id_token")]
    public string IdToken { get; set; }
}