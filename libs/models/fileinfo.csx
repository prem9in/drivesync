#r "Microsoft.WindowsAzure.Storage"

#load "./filetype.csx"
#load "./onedrive.csx"
#load "./../common/extension.csx"

using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;


public class BlobInfo : TableEntity
{
    public BlobInfo()
    {
    }

    public BlobInfo(FileInfo file)
    {
        this.PartitionKey = "BlobFiles";
        this.RowKey = file.RowKey;
        this.Name = file.Name;
        this.SyncId = file.SyncId;
        this.Type = file.Type;
        this.ETag = "*";
    }

    public string Name { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid SyncId { get; set; }
    public string Type { get; set; }
    public string Path { get; set; }
}

public class FileInfo : TableEntity
{
    public FileInfo()
    {
    }

    public FileInfo(Guid syncId, OneDriveItem driveItem, DateTime timestamp)
    {
        this.PartitionKey = "DriveFiles";
        this.Id = driveItem.Id;
        this.RowKey = NormalizeRowKey(driveItem.Id);
        this.Name = driveItem.FileName;
        this.Extension = driveItem.FileExtension;
        this.FullPath = driveItem.FullPath;
        this.LastModified = driveItem.LastModifiedDateTime;
        this.Size = driveItem.Size;
        this.MimeType = driveItem.MimeType;
        this.LastModifiedBy = driveItem.LastModifiedBy == null ? string.Empty : (driveItem.LastModifiedBy.User == null ? string.Empty : driveItem.LastModifiedBy.User.DisplayName);
        this.SyncId = syncId;
        this.Type = driveItem.Type.ToString();
        this.Timestamp = timestamp;
        this.Blobed = false;
        this.ETag = "*";
    }
    
    public string Id { get; set; }
    public string Name { get; set; }
    public string Extension { get; set; }
    public string FullPath { get; set; }    
    public DateTimeOffset LastModified { get; set; }
    public long Size { get; set; }
    public string MimeType { get; set; }      
    public string LastModifiedBy { get; set; }    
    public Guid SyncId { get; set; }
    public string Type { get; set; }
    public bool Blobed { get; set; }

    public override string ToString()
    {
        return string.Format("Parition Key: {0}, RowKey: {1}, Id: {2}, Type: {3}, FullPath: {4}, Size: {5}, MimeType: {6}",
            this.PartitionKey,
            this.RowKey,
            this.Id,
            this.Type,
            this.FullPath,
            this.Size,
            this.MimeType);
    }
}

// Custom comparer for the Product class
public class FileInfoComparer : IEqualityComparer<FileInfo>
{
    public bool Equals(FileInfo x, FileInfo y)
    {
        return x.RowKey == y.RowKey;
    }

    public int GetHashCode(FileInfo x)
    {
       return x.RowKey.GetHashCode(); 
    }
}

public class AudioInfo : TableEntity
{
    public AudioInfo()
    {
    }

    public AudioInfo(Guid syncId, OneDriveItem driveItem, DateTime timestamp)
    {
        this.PartitionKey = "AudioFiles";
        this.RowKey = NormalizeRowKey(driveItem.Id);
        this.Id = driveItem.Id;
        this.Type = driveItem.Type.ToString();
        this.SyncId = syncId;
        this.TakenDateTime = driveItem.Audio.TakenDateTime;
        this.Album = driveItem.Audio.Album;
        this.AlbumArtist = driveItem.Audio.AlbumArtist;
        this.Artist = driveItem.Audio.Artist;
        this.Bitrate = driveItem.Audio.Bitrate;
        this.Copyright = driveItem.Audio.Copyright;
        this.Title = driveItem.Audio.Title;
        this.Track = driveItem.Audio.Track;
        this.Year = driveItem.Audio.Year;
        this.Genre = driveItem.Audio.Genre;
        this.HasDrm = driveItem.Audio.HasDrm;
        this.IsVariableBitrate = driveItem.Audio.IsVariableBitrate;
        this.Duration = driveItem.Audio.Duration;
        this.Timestamp = timestamp;
        this.ETag = "*";
    }

    public string Id { get; set; }

    public Guid SyncId { get; set; }

    public string Type { get; set; }

    public DateTimeOffset TakenDateTime { get; set; }
        
    public string Album { get; set; }

    public string AlbumArtist { get; set; }

    public string Artist { get; set; }

    public int Bitrate { get; set; }

    public string Copyright { get; set; }

    public string Title { get; set; }

    public int Track { get; set; }

    public int Year { get; set; }

    public string Genre { get; set; }

    public bool HasDrm { get; set; }

    public bool IsVariableBitrate { get; set; }

    public int Duration { get; set; }
}

public class PhotoInfo : TableEntity
{
    public PhotoInfo()
    {
    }

    public PhotoInfo(Guid syncId, OneDriveItem driveItem, DateTime timestamp)
    {
        this.PartitionKey = "PhotoFiles";
        this.RowKey = NormalizeRowKey(driveItem.Id);
        this.Id = driveItem.Id;
        this.Type = driveItem.Type.ToString();
        this.SyncId = syncId;
        this.Timestamp = timestamp;
        this.ETag = "*";
        if (driveItem.Image != null)
        {
            this.Width = driveItem.Image.Width;
            this.Height = driveItem.Image.Height;
        }

        if (driveItem.Photo != null)
        {
            this.TakenDateTime = driveItem.Photo.TakenDateTime;
            this.CameraMake = driveItem.Photo.CameraMake;
            this.CameraModel = driveItem.Photo.CameraModel;
            this.Iso = driveItem.Photo.Iso;
            this.FocalLength = driveItem.Photo.FocalLength;
            this.FNumber = driveItem.Photo.FNumber;
        }
    }

    public string Id { get; set; }

    public Guid SyncId { get; set; }

    public int Width { get; set; }
    
    public int Height { get; set; }

    public DateTimeOffset TakenDateTime { get; set; }

    public string CameraMake { get; set; }

    public string CameraModel { get; set; }

    public long Iso { get; set; }

    public double FocalLength { get; set; }

    public double FNumber { get; set; }

    public string Type { get; set; }
}

public class VideoInfo : TableEntity
{
    public VideoInfo()
    {
    }

    public VideoInfo(Guid syncId, OneDriveItem driveItem, DateTime timestamp)
    {
        this.PartitionKey = "VideoFiles";
        this.RowKey = NormalizeRowKey(driveItem.Id);
        this.Id = driveItem.Id;
        this.Type = driveItem.Type.ToString();
        this.SyncId = syncId;
        this.BitRate = driveItem.Video.BitRate;
        this.Width = driveItem.Video.Width;
        this.Height = driveItem.Video.Height;
        this.Duration = driveItem.Video.Duration;
        this.Timestamp = timestamp;
        this.ETag = "*";
    }

    public string Id { get; set; }

    public int BitRate { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int Duration { get; set; }

    public string Type { get; set; }

    public Guid SyncId { get; set; }
}

public class LocationInfo : TableEntity
{
    public LocationInfo()
    {
    }

    public LocationInfo(Guid syncId, OneDriveItem driveItem, DateTime timestamp)
    {
        this.PartitionKey = "FileLocation";
        this.RowKey = NormalizeRowKey(driveItem.Id);
        this.Id = driveItem.Id;
        this.SyncId = syncId;
        this.Altitude = driveItem.Location.Altitude;
        this.Latitude = driveItem.Location.Latitude;
        this.Longitude = driveItem.Location.Longitude;
        this.Timestamp = timestamp;
        this.ETag = "*";
    }

    public string Id { get; set; }

    public double Altitude { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public Guid SyncId { get; set; }
}

