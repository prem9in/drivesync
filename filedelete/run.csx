#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

#load "./../libs/common/onedrive.csx"
#load "./../libs/models/authinfo.csx"
#load "./../libs/models/runtime.csx"
#load "./../libs/models/onedrive.csx"

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

public static async void Run(IQueryable<AuthInfo> authConnect,
    CloudTable onedriveConnect,
    OneDriveItem deleteItem,   
    TraceWriter log)
{
    var runtime = new Runtime {
        Log = log,
        AuthConnect = authConnect,
        OnedriveConnect = onedriveConnect
    };
    var startTime = DateTime.UtcNow;
    log.Info("Start Time: " + startTime);

    await OneDrive.DeleteFile(runtime, deleteItem);

    var elapsed = DateTime.UtcNow - startTime;
    log.Info("Duration: " + elapsed);
}
