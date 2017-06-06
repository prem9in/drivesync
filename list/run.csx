#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

#load "./../libs/models/keys.csx"
#load "./../libs/common/blob.csx"
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
    CloudTable fileInfoTable,
    TraceWriter log)
{
    var runtime = new Runtime
    {
        Request = request,
        Log = log,      
      
    };
    var start = DateTime.UtcNow;
    log.Info("Start : " + start);
    // Construct the query operation for all customer entities where PartitionKey="Smith".
    TableQuery<FileInfo> fileQuery = new TableQuery<FileInfo>().Where(TableQuery.CombineFilters(
        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "DriveFiles"),
        TableOperators.And,
        TableQuery.GenerateFilterConditionForBool("Blobed", QueryComparisons.Equal, true)));
    TableQuery<PhotoInfo> photoQuery = new TableQuery<PhotoInfo>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "PhotoFiles"));
    var taskLists = new List<Task>();
    var existingFiles = Task.Run(() => { return fileInfoTable.ExecuteQuery(fileQuery).Select(f => new { f.Id, f.Extension, f.FullPath, f.MimeType, f.LastModifiedBy, f.LastModified, f.Size, f.Type, f.Name }); });
    taskLists.Add(existingFiles);
    var pFiles = Task.Run(() => { return fileInfoTable.ExecuteQuery(photoQuery).Select(f => new { f.Id, f.CameraMake, f.CameraModel, f.FNumber, f.FocalLength, f.Height, f.Iso, f.TakenDateTime, f.Width }); });
    taskLists.Add(pFiles);
    var keys = new Keys();
    var thumbToken = Task.Run(() => { return BlobDrive.GetKey(runtime, true); });
    taskLists.Add(thumbToken);
    var driveToken = Task.Run(() => { return BlobDrive.GetKey(runtime, false); });
    taskLists.Add(driveToken);
    /*
    var tablePolicy = new SharedAccessTablePolicy()
    {
        Permissions = SharedAccessTablePermissions.Query,
        SharedAccessExpiryTime = DateTime.UtcNow.AddDays(1)
    };
    var listToken = Task.Run(() => { return fileInfoTable.GetSharedAccessSignature(tablePolicy); });
    taskLists.Add(listToken);
    */
    await Task.WhenAll(taskLists);
    var elapsed = DateTime.UtcNow - start;
    log.Info("Duration : " + elapsed);
    var result = new { DriveToken = driveToken.Result, ThumbToken = thumbToken.Result, Files = existingFiles.Result, Photos = pFiles.Result };   
    return request.CreateResponse(HttpStatusCode.OK, result, new JsonMediaTypeFormatter());
}
