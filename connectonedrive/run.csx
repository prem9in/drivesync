#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

#load "./../libs/common/oauth.csx"
#load "./../libs/models/authinfo.csx"
#load "./../libs/models/runtime.csx"

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


public static async Task<HttpResponseMessage> Run(HttpRequestMessage request, CloudTable onedriveConnect, TraceWriter log)
{
    var runtime = new Runtime { Request = request, OnedriveConnect = onedriveConnect, Log = log };
    if (request.Method == HttpMethod.Get)
    {
        return OAuth.RedirectToOAuth(runtime);
    }
    else if (request.Method == HttpMethod.Post)
    {
        return await OAuth.CreateConnection(runtime);
    }

    return request.CreateResponse(HttpStatusCode.OK, string.Empty, new JsonMediaTypeFormatter());
}
