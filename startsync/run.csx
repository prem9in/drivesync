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
    CloudTable fileInfoTable,
    CloudTable syncInfoTable,
    CloudTable onedriveConnect,
    IQueryable<FileInfo> fileInfoMeta,
    IAsyncCollector<FileInfo> outputQueue,
    TraceWriter log)
{
    var runtime = new Runtime {
        Request = request,
        Log = log,
        AuthConnect = authConnect,
        FileInfoTable = fileInfoTable,
        SyncInfoTable = syncInfoTable,
        OnedriveConnect = onedriveConnect,
        FileInfoMeta = fileInfoMeta,
        OutputQueue = outputQueue
    };
    var startTime = DateTime.UtcNow;
    log.Info("Start Time: " + startTime);   
    var allFiles = await OneDrive.GetAllFiles(runtime);
    var syncInfo = await Sync.Start(runtime, allFiles, startTime);
    var elapsed = DateTime.UtcNow - startTime;
    log.Info("Duration: " + elapsed);
    return request.CreateResponse(HttpStatusCode.OK, syncInfo, new JsonMediaTypeFormatter());
}
