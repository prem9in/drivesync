using System.Collections.Specialized;

public class OAuthCodeResponse
{
    public string Code { get; set; }
    public bool AdminConsent { get; set; }
    public string SessionState { get; set; }
    public string State { get; set; }

    public OAuthCodeResponse(NameValueCollection item)
    {
        this.Code = item["code"];
        this.AdminConsent = item["admin_consent"] == "True";
        this.SessionState = item["session_state"];
        this.State = item["state"];
    }
}