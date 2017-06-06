#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

#load "./../libs/common/sync.csx"
#load "./../libs/common/onedrive.csx"
#load "./../libs/models/syncinfo.csx"
#load "./../libs/models/authinfo.csx"
#load "./../libs/models/runtime.csx"
#load "./../libs/models/fileinfo.csx"

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Formatting;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage request,
    IQueryable<AuthInfo> authConnect,
    CloudTable onedriveConnect,
    IQueryable<FileInfo> fileInfoMeta,
    IQueryable<PhotoInfo> photoInfoMeta, 
    IAsyncCollector<OneDriveItem> deleteQueue,
    TraceWriter log)
{
    var runtime = new Runtime {
        Request = request,
        Log = log,
        AuthConnect = authConnect,
        OnedriveConnect = onedriveConnect,
        FileInfoMeta = fileInfoMeta,
        DeleteQueue = deleteQueue,
        PhotoInfoMeta = photoInfoMeta
    };
    var startTime = DateTime.UtcNow;
    log.Info("Start Time: " + startTime);   
    var allFiles = await OneDrive.GetAllFiles(runtime);
    var filesToDelete = Sync.StartDelete(runtime, allFiles, startTime);
    var elapsed = DateTime.UtcNow - startTime;
    log.Info("Duration: " + elapsed);
    log.Info("To be Deleted : " + filesToDelete.Count);
    return request.CreateResponse(HttpStatusCode.OK, filesToDelete.Select(f => new { f.Name, f.Type, f.FullPath, f.LastModifiedDateTime }), new JsonMediaTypeFormatter());
}
