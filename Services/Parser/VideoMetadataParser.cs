using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.VisualBasic;
using YtDlpDownloader.Models;

namespace YtDlpDownloader.Services.Parser;

public class VideoMetadataParser : IMetadataParser
{
    public VideoInfo Parse(JsonElement root)
    {
        var title = root.TryGetProperty("title", out var t) ? t.GetString() : "Без названия";
        var author = root.TryGetProperty("uploader", out var u) ? u.GetString() : "Без автора";
        var duration = root.TryGetProperty("duration_string", out var d) ? d.GetString() : "00:00";
        var thumbnail = root.TryGetProperty("thumbnail", out var th) ? th.GetString() : string.Empty;

        var formatsList = new List<VideoFormat>();

        if (root.TryGetProperty("formats", out var formatsProp) &&
        formatsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in formatsProp.EnumerateArray())
            {
                var resolution = f.TryGetProperty("resolution", out var res) ? res.GetString() : string.Empty;
                if (string.IsNullOrEmpty(resolution) || resolution == "audio only")
                    continue;
                
                var formatId = f.TryGetProperty("format_id", out var id) ? id.GetString() : string.Empty;
                var ext = f.TryGetProperty("ext", out var extension) ? extension.GetString() : string.Empty;

                var acodec = f.TryGetProperty("acodec", out var ac) ? ac.GetString() : string.Empty;
                var vcodec = f.TryGetProperty("vcodec", out var vc) ? vc.GetString() : string.Empty;
                bool isVideoOnly = acodec == "none" && vcodec != "none";

                long? fileSize = f.TryGetProperty("filesize", out var fs) && 
                    fs.ValueKind == JsonValueKind.Number ? fs.GetInt64() : null;
                long? approx = f.TryGetProperty("filesize_approx", out var approxProp) &&
                    approxProp.ValueKind == JsonValueKind.Number ? approxProp.GetInt64() : null;

                formatsList.Add(new VideoFormat
                {
                    FormatId = formatId ?? string.Empty,
                    Extension = ext ?? string.Empty,
                    Resolution = resolution,
                    FileSize = FormatBytes(fileSize ?? approx),
                    IsVideoOnly = isVideoOnly
                });
            }
        }

        return new VideoInfo
        {
            Title = title ?? "Без названия",
            Author = author ?? "Неизвестый автор",
            Duration = duration ?? "00:00",
            ThumbnailUrl = thumbnail ?? string.Empty,
            AvailableFormats = formatsList
        };
    }

    private static string FormatBytes(long? bytes)
    {
        if (bytes == null)
            return "Размер неизвестен";
        string[] suffix = { "B", "KB", "MG", "GB", "TB" };
        int i;
        double dblSbyte = bytes.Value;
        for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            dblSbyte = bytes.Value / 1024.0;
        
        return $"{dblSbyte:0.##} {suffix[i]}";
    }
}