#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

public class SyncInfo : TableEntity
{   
   public int Count { get; set; }
   public TimeSpan Duration { get; set; }
   public ulong Size { get; set; }   
}