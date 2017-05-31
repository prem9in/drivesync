#r "Newtonsoft.Json"

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;

public class OAuthErrorResponse
{
    [JsonProperty(PropertyName = "error")]
    public string Error { get; set; }
    [JsonProperty(PropertyName = "error_description")]
    public string ErrorDescription { get; set; }
    [JsonProperty(PropertyName = "correlation_id")]
    public string State { get; set; }

    public OAuthErrorResponse(NameValueCollection item)
    {
        this.Error = item["error"];
        this.ErrorDescription = item["error_description"];
        this.State = item["state"];        
    }
}