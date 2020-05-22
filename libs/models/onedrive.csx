#r "Newtonsoft.Json"

#load "./../common/mimetypemap.csx"
#load "./filetype.csx"

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.IO;

public class ThumbNails
{
    [JsonProperty(PropertyName = "value")]
    public List<ThumbNail> Items { get; set; }
}

public class ThumbNail
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "small")]
    public ThumbItem Small { get; set; }

    [JsonProperty(PropertyName = "medium")]
    public ThumbItem Medium { get; set; }

    [JsonProperty(PropertyName = "large")]
    public ThumbItem Large { get; set; }
}

public class ThumbItem
{
    [JsonProperty(PropertyName = "height")]
    public int Height { get; set; }

    [JsonProperty(PropertyName = "width")]
    public int Width { get; set; }

    [JsonProperty(PropertyName = "url")]
    public string Url { get; set; }
}

public class DriveItems
{
    [JsonProperty(PropertyName = "@odata.nextLink")]
    public string NextLink { get; set; }

    [JsonProperty(PropertyName = "value")]
    public List<OneDriveItem> Items { get; set; }
}

//// see defintions https://docs.microsoft.com/en-us/graph/api/resources/driveitem?view=graph-rest-1.0

public class OneDriveItem
{
    [JsonIgnore]
    public string FullPath
    {
        get
        {
            var result = string.Empty;
            if (this.Parent != null && !string.IsNullOrWhiteSpace(this.Parent.Path))
            {
               result = this.Parent.Path.Replace("/drive/root:", string.Empty) + "/" + this.Name;
            }

            return result;
        }
    }

    [JsonIgnore]
    public FileType Type
    {
        get
        {
            var result = FileType.None;
            if (this.IsFile)
            {
                result = FileType.File;
            }

            if (this.IsVideo)
            {
                result = FileType.Video;
            }
            else if (this.IsAudio)
            {
                result = FileType.Audio;
            }
            else if (this.IsImage || this.IsPhoto)
            {
                result = FileType.Photo;
            } 
            
            return result;
        }
    }

    [JsonIgnore]
    public bool IsFolder
    {
        get
        {
            return this.Folder != null;
        }
    }

    [JsonIgnore]
    public bool IsDeleted
    {
        get
        {
            return this.Deleted != null;
        }
    }

    [JsonIgnore]
    public bool IsFile
    {
        get
        {
            return this.File != null;
        }
    }

    [JsonIgnore]
    public bool IsImage
    {
        get
        {
            return this.Image != null;
        }
    }

    [JsonIgnore]
    public bool IsPhoto
    {
        get
        {
            return this.Photo != null;
        }
    }

    [JsonIgnore]
    public bool IsAudio
    {
        get
        {
            return this.Audio != null;
        }
    }

    [JsonIgnore]
    public bool IsVideo
    {
        get
        {
            return this.Video != null;
        }
    }

    [JsonIgnore]
    public string FolderName
    {
        get
        {
            var result = string.Empty;
            if (this.IsFolder)
            {
                result = this.Name.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Last();                
            }
            return result;
        }
    }

    [JsonIgnore]
    public string FileExtension
    {
        get
        {
            var result = string.Empty;
            if (this.IsFile)
            {
                string fileName = this.Name.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Last();
                var fileNameParts = fileName.Split(new char[] { '.' });
                result = fileNameParts.Length > 1 ? fileNameParts.Last() : string.Empty;
            }
            return result;
        }
    }

    [JsonIgnore]
    public string FileName
    {
        get
        {
            var result = string.Empty;
            if (this.IsFile)
            {
                string fileName = this.Name.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Last();
                var fileNameParts = fileName.Split(new char[] { '.' });
                result = fileNameParts.First();
            }
            return result;
        }
    }

    [JsonIgnore]
    public string MimeType
    {
        get
        {
            var result = string.Empty;
            if (this.IsFile)
            {
                if (string.IsNullOrWhiteSpace(this.File.MimeType))
                {
                    result = MimeTypeMap.GetMimeTypeFromExtension(this.FileExtension);
                }
                else
                {
                    result = this.File.MimeType;
                }
            }
            return result;
        }
    }

    [JsonProperty(PropertyName = "deleted")]
    public DeletedFacet Deleted { get; set; }

    [JsonProperty(PropertyName = "description")]
    public string Description { get; set; }

    [JsonProperty(PropertyName = "file")]
    public FileFacet File { get; set; }

    [JsonProperty(PropertyName = "folder")]
    public FolderFacet Folder { get; set; }

    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "lastModifiedBy")]
    public OneDriveUser LastModifiedBy { get; set; }

    [JsonProperty(PropertyName = "lastModifiedDateTime")]
    public DateTimeOffset LastModifiedDateTime { get; set; }

    [JsonProperty(PropertyName = "location")]
    public LocationFacet Location { get; set; }

    [JsonProperty(PropertyName = "image")]
    public ImageFacet Image { get; set; }

    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "package")]
    public PackageFacet Package { get; set; }

    [JsonProperty(PropertyName = "parentReference")]
    public ItemReference Parent { get; set; }

    [JsonProperty(PropertyName = "photo")]
    public PhotoFacet Photo { get; set; }

    [JsonProperty(PropertyName = "audio")]
    public AudioFacet Audio { get; set; }

    [JsonProperty(PropertyName = "root")]
    public RootFacet Root { get; set; }

    [JsonProperty(PropertyName = "size")]
    public long Size { get; set; }

    [JsonProperty(PropertyName = "video")]
    public VideoFacet Video { get; set; }

    [JsonProperty(PropertyName = "@microsoft.graph.downloadUrl")]
    public string ShortLivedDownloadUrl { get; set; }
}

public class AudioFacet
{
    [JsonProperty(PropertyName = "takenDateTime")]
    public DateTimeOffset TakenDateTime { get; set; }

    [JsonProperty(PropertyName = "album")]
    public string Album { get; set; }

    [JsonProperty(PropertyName = "albumArtist")]
    public string AlbumArtist { get; set; }

    [JsonProperty(PropertyName = "artist")]
    public string Artist { get; set; }

    [JsonProperty(PropertyName = "bitrate")]
    public int Bitrate { get; set; }

    [JsonProperty(PropertyName = "copyright")]
    public string Copyright { get; set; }

    [JsonProperty(PropertyName = "title")]
    public string Title { get; set; }

    [JsonProperty(PropertyName = "track")]
    public int Track { get; set; }

    [JsonProperty(PropertyName = "year")]
    public int Year { get; set; }

    [JsonProperty(PropertyName = "genre")]
    public string Genre { get; set; }

    [JsonProperty(PropertyName = "hasDrm")]
    public bool HasDrm { get; set; }

    [JsonProperty(PropertyName = "isVariableBitrate")]
    public bool IsVariableBitrate { get; set; }

    [JsonProperty(PropertyName = "duration")]
    public int Duration { get; set; }
}


public class VideoFacet
{
    [JsonProperty(PropertyName = "bitrate")]
    public int BitRate { get; set; }

    [JsonProperty(PropertyName = "width")]
    public int Width { get; set; }

    [JsonProperty(PropertyName = "height")]
    public int Height { get; set; }

    [JsonProperty(PropertyName = "duration")]
    public int Duration { get; set; }
}

public class RootFacet
{
}


public class PhotoFacet
{
    [JsonProperty(PropertyName = "takenDateTime")]
    public DateTimeOffset TakenDateTime { get; set; }

    [JsonProperty(PropertyName = "cameraMake")]
    public string CameraMake { get; set; }

    [JsonProperty(PropertyName = "cameraModel")]
    public string CameraModel { get; set; }

    [JsonProperty(PropertyName = "iso")]
    public long Iso { get; set; }

    [JsonProperty(PropertyName = "focalLength")]
    public double FocalLength { get; set; }

    [JsonProperty(PropertyName = "fNumber")]
    public double FNumber { get; set; }
}

public class ItemReference
{
    [JsonProperty(PropertyName = "driveId")]
    public string DriveId { get; set; }

    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "path")]
    public string Path { get; set; }

    [JsonProperty(PropertyName = "shareId")]
    public string ShareId { get; set; }
}

public class ImageFacet
{
    [JsonProperty(PropertyName = "width")]
    public int Width { get; set; }

    [JsonProperty(PropertyName = "height")]
    public int Height { get; set; }
}

public class LocationFacet
{
    [JsonProperty(PropertyName = "altitude")]
    public double Altitude { get; set; }

    [JsonProperty(PropertyName = "latitude")]
    public double Latitude { get; set; }

    [JsonProperty(PropertyName = "longitude")]
    public double Longitude { get; set; }
}

public class FolderFacet
{
    [JsonProperty(PropertyName = "childCount")]
    public long ChildCount { get; set; }
}

public class PackageFacet
{
    [JsonProperty(PropertyName = "type")]
    public string Type { get; set; }
}

public class FileFacet
{
    [JsonProperty(PropertyName = "mimeType")]
    public string MimeType { get; set; }
}

public class DeletedFacet
{
    [JsonProperty(PropertyName = "state")]
    public string State { get; set; }
}

public class OneDriveUser
{
    [JsonProperty(PropertyName = "user")]
    public OneDriveIdentity User { get; set; }
}

public class OneDriveIdentity
{
    [JsonProperty(PropertyName = "displayName")]
    public string DisplayName { get; set; }

    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
}