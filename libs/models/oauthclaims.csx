#r "Newtonsoft.Json"

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class OAuthClaims
{
    [JsonProperty(PropertyName = "aud")]
    public string Audience { get; set; }
    [JsonProperty(PropertyName = "exp")]
    public int Expiration { get; set; }
    [JsonProperty(PropertyName = "iss")]
    public string Issuer { get; set; }
    [JsonProperty(PropertyName = "iat")]
    public string IssuedAtTime { get; set; }
    [JsonProperty(PropertyName = "nbf")]
    public string NotBeforeTime { get; set; }
    [JsonProperty(PropertyName = "oid")]
    public string ObjectIdentifier { get; set; }
    [JsonProperty(PropertyName = "tid")]
    public string TenantIdentifier { get; set; }
    [JsonProperty(PropertyName = "sub")]
    public string SubjectIdentifier { get; set; }
    [JsonProperty(PropertyName = "upn")]
    public string PrincipalName { get; set; }
    [JsonProperty(PropertyName = "unique_name")]
    public string UniqueName { get; set; }
    [JsonProperty(PropertyName = "ver")]
    public string Version { get; set; }
}