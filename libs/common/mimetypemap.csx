using System;
using System.Collections.Generic;

public static class MimeTypeMap
{
    /// <summary>
    /// Dictionary of Mime Mappings
    /// </summary>
    private static readonly IDictionary<string, string> Mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
                { ".3g2", "video/3gpp2" },
                { ".3gp", "video/3gpp" },
                { ".3gp2", "video/3gpp2" },
                { ".3gpp", "video/3gpp" },
                { ".atom", "application/atom+xml" },
                { ".css", "text/css" },
                { ".csv", "text/csv" },
                { ".doc", "application/msword" },
                { ".docm", "application/vnd.ms-word.document.macroEnabled.12" },
                { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                { ".dot", "application/msword" },
                { ".dotm", "application/vnd.ms-word.template.macroEnabled.12" },
                { ".flv", "video/x-flv" },
                { ".jpe", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".jpg", "image/jpeg" },
                { ".mov", "video/quicktime" },
                { ".movie", "video/x-sgi-movie" },
                { ".mp2", "video/mpeg" },
                { ".mp2v", "video/mpeg" },
                { ".mp3", "audio/mpeg" },
                { ".mp4", "video/mp4" },
                { ".mp4v", "video/mp4" },
                { ".mpa", "video/mpeg" },
                { ".mpe", "video/mpeg" },
                { ".mpeg", "video/mpeg" },
                { ".one", "application/onenote" },
                { ".onea", "application/onenote" },
                { ".onepkg", "application/onenote" },
                { ".onetmp", "application/onenote" },
                { ".onetoc", "application/onenote" },
                { ".onetoc2", "application/onenote" },
                { ".gif", "image/gif" },
                { ".png", "image/png" },
                { ".pnz", "image/png" },
                { ".pot", "application/vnd.ms-powerpoint" },
                { ".potm", "application/vnd.ms-powerpoint.template.macroEnabled.12" },
                { ".potx", "application/vnd.openxmlformats-officedocument.presentationml.template" },
                { ".ppa", "application/vnd.ms-powerpoint" },
                { ".ppam", "application/vnd.ms-powerpoint.addin.macroEnabled.12" },
                { ".ppm", "image/x-portable-pixmap" },
                { ".pps", "application/vnd.ms-powerpoint" },
                { ".ppsm", "application/vnd.ms-powerpoint.slideshow.macroEnabled.12" },
                { ".ppsx", "application/vnd.openxmlformats-officedocument.presentationml.slideshow" },
                { ".ppt", "application/vnd.ms-powerpoint" },
                { ".pptm", "application/vnd.ms-powerpoint.presentation.macroEnabled.12" },
                { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
                { ".svg", "image/svg+xml" },
                { ".swf", "application/x-shockwave-flash" },
                { ".txt", "text/plain" },
                { ".wsc", "text/scriptlet" },
                { ".wsdl", "text/xml" },
                { ".xaml", "application/xaml+xml" },
                { ".xlam", "application/vnd.ms-excel.addin.macroEnabled.12" },
                { ".xlc", "application/vnd.ms-excel" },
                { ".xld", "application/vnd.ms-excel" },
                { ".xlk", "application/vnd.ms-excel" },
                { ".xll", "application/vnd.ms-excel" },
                { ".xlm", "application/vnd.ms-excel" },
                { ".xls", "application/vnd.ms-excel" },
                { ".xlsb", "application/vnd.ms-excel.sheet.binary.macroEnabled.12" },
                { ".xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12" },
                { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                { ".xlt", "application/vnd.ms-excel" },
                { ".xltm", "application/vnd.ms-excel.template.macroEnabled.12" },
                { ".xltx", "application/vnd.openxmlformats-officedocument.spreadsheetml.template" },
                { ".xlw", "application/vnd.ms-excel" },
                { ".xml", "text/xml" },
                { ".zip", "application/x-zip-compressed" },
             };

    /// <summary>
    /// Method to get the Mime Type
    /// </summary>
    /// <param name="extension">extension of the file</param>
    /// <returns>The Content Type</returns>
    public static string GetMimeTypeFromExtension(string extension)
    {
        if (extension != null)
        {
            if (!extension.StartsWith(".", StringComparison.OrdinalIgnoreCase))
            {
                extension = "." + extension;
            }

            if (Mappings.ContainsKey(extension))
            {
                return Mappings[extension];
            }
        }

        return "application/octet-stream";
    }
}
