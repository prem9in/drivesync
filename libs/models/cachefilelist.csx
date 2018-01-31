public class CacheResult 
{
    public Dictionary<string, object> AllPhotofiles {get; set;} 
    public Dictionary<string, object> AllVideofiles {get; set;}
    public IEnumerable<dynamic> AllFiles {get; set;}
    public string ThumbToken { get; set;}
    public string ThumbUri { get; set;}
    public string DriveToken { get; set;}
    public string DriveUri { get; set;}
    public int TotalCount {get; set;}
}
