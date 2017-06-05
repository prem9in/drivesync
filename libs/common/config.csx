using System.Configuration;

public static class AppConfiguration
{
    private static string Get(string key)
    {
        return ConfigurationManager.AppSettings[key];
    }

    public static string AzureWebJobsStorage = Get("AzureWebJobsStorage");
    public static string AzureWebJobsDashboard = Get("AzureWebJobsDashboard");
    public static string WEBSITE_CONTENTAZUREFILECONNECTIONSTRING = Get("WEBSITE_CONTENTAZUREFILECONNECTIONSTRING");
    public static string OAuth_BaseUri = Get("OAuthBase");
    public static string OAuth_ClientId = Get("ClientId");
    public static string OAuth_ResponseType = Get("ResponseType");
    public static string OAuth_RedirectUrl = Get("RedirectUrl");
    public static string OAuth_ResponseMode = Get("ResponseMode");
    public static string OAuth_DomainHint = Get("DomainHint");
    public static string OAuth_Scope = Get("Scope");
    public static string OAuth_ClientSecret = Get("ClientSecret");
    public static string OAuth_GrantType = Get("GrantType");
    public static string OAuth_AuthorizeFormat = Get("AuthorizeFormat");
    public static string OAuth_TokenFormat = Get("TokenFormat");
    public static string OAuth_TokenPostFormat = Get("TokenPostFormat");
    public static string OAuthDefaultUser = Get("OAuthDefaultUser");
    public static string OAuth_GrantTypeRefreshToken = Get("GrantTypeRefreshToken");
    public static string OAuth_RefreshTokenPostFormat = Get("RefreshTokenPostFormat");
    public static string OneDriveFolderFormat = Get("OneDriveFolderFormat");
    public static string OneDriveRootFormat = Get("OneDriveRootFormat");
    public static string OneDriveBaseUri = Get("OneDriveBaseUri");
    public static string OneDriveFileContentFormat = Get("OneDriveFileContentFormat");
    public static string OneDriveFileThumbnailFormat = Get("OneDriveFileThumbnailFormat");
    public static string BDriveStorage = Get("BDriveStorage");
    public static string DriveContainer = Get("DriveContainer");
    public static string DriveThumbContainer = Get("DriveThumbContainer");
    public static ulong MaxAllowedSize = ulong.Parse(Get("MaxAllowedSize"));
    public static ulong SizeAfterDelete = ulong.Parse(Get("SizeAfterDelete"));
    public static string OneDriveFileDeleteFormat = Get("OneDriveFileDeleteFormat");
}

