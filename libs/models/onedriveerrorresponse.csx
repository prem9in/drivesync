#r "Newtonsoft.Json"

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;

public class OneDriveError
{
    [JsonProperty(PropertyName = "code")]
    public string Code { get; set; }

    [JsonProperty(PropertyName = "message")]
    public string message { get; set; }

    [JsonProperty(PropertyName = "innererror")]
    public OneDriveError InnerError { get; set; }
}

public class OneDriveErrorResponse
{
    [JsonProperty(PropertyName = "error")]
    public OneDriveError Error { get; set; }
}