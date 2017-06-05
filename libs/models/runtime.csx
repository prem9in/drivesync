#r "Microsoft.WindowsAzure.Storage"

#load "./authinfo.csx"
#load "./fileinfo.csx"
#load "./syncinfo.csx"
#load "./onedrive.csx"

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Specialized;
using Microsoft.WindowsAzure.Storage.Table;


public class Runtime
{
    public HttpRequestMessage Request { get; set; }
    public IQueryable<AuthInfo> AuthConnect { get; set; }
    public CloudTable OnedriveConnect { get; set; }
    public CloudTable FileInfoTable { get; set; }
    public CloudTable SyncInfoTable { get; set; }
    public IQueryable<FileInfo> FileInfoMeta { get; set; }
    public IAsyncCollector<FileInfo> OutputQueue { get; set; }
    public IAsyncCollector<OneDriveItem> DeleteQueue { get; set; }
    public TraceWriter Log { get; set; }
}