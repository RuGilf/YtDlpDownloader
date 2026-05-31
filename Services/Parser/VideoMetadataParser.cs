using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using YtDlpDownloader.Models;

namespace YtDlpDownloader.Services.Parser;

public class VideoMetadataParser : IMetadataParser
{
    public VideoInfo Parse(JsonElement root)
    {
        var title = root.TryGetProperty("title", out var t) ? t.GetString() : "Без названия";
        var author = root.TryGetProperty("uploader", out var u) ? u.GetString() : "Неизвестный автор";
        var duration = root.TryGetProperty("duration_string", out var d) ? d.GetString() : "00:00";
        var thumbnail = root.TryGetProperty("thumbnail", out var th) ? th.GetString() : string.Empty;

        var resolutions = new List<int>();
        var containers = new List<string>();
        var audioTracks = new List<string> { "Оригинал" };
        var subtitles = new List<string> { "Без субтитров" };

        if (root.TryGetProperty("formats", out var formatsProp) && formatsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in formatsProp.EnumerateArray())
            {
                var vcodec = f.TryGetProperty("vcodec", out var vc) ? vc.GetString() : string.Empty;
                var acodec = f.TryGetProperty("acodec", out var ac) ? ac.GetString() : string.Empty;

                if (vcodec != "none" && f.TryGetProperty("height", out var hProp) && hProp.ValueKind == JsonValueKind.Number)
                {
                    int height = hProp.GetInt32();
                    if (height > 0 && !resolutions.Contains(height))
                    {
                        resolutions.Add(height);
                    }
                }

                if (vcodec != "none" && f.TryGetProperty("ext", out var extProp))
                {
                    var ext = extProp.GetString()?.ToUpper();
                    if (!string.IsNullOrEmpty(ext) && !containers.Contains(ext))
                    {
                        containers.Add(ext);
                    }
                }

                if (vcodec == "none" && acodec != "none" && f.TryGetProperty("language", out var langProp))
                {
                    var lang = langProp.GetString();
                    if (!string.IsNullOrEmpty(lang) && lang != "none" && !audioTracks.Contains(lang))
                    {
                        audioTracks.Add(lang);
                    }
                }
            }
        }

        var sortedResolutions = resolutions
            .OrderByDescending(r => r)
            .Select(r => $"{r}p")
            .ToList();
        
        sortedResolutions.Insert(0, "Максимальное"); 

        if (!containers.Contains("MP4")) containers.Add("MP4");
        if (!containers.Contains("MKV")) containers.Add("MKV");
        containers.Add("MP3"); 

        if (root.TryGetProperty("subtitles", out var subsProp) && subsProp.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in subsProp.EnumerateObject())
            {
                if (!subtitles.Contains(prop.Name)) subtitles.Add(prop.Name);
            }
        }

        if (root.TryGetProperty("automatic_captions", out var autoProp) && autoProp.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in autoProp.EnumerateObject())
            {
                var langCode = prop.Name;
                if (!subtitles.Contains(langCode)) subtitles.Add(langCode + " (авто)");
            }
        }

        return new VideoInfo
        {
            Title = title ?? "Без названия",
            Author = author ?? "Неизвестный автор",
            Duration = duration ?? "00:00",
            ThumbnailUrl = thumbnail ?? string.Empty,
            AvailableResolutions = sortedResolutions,
            AvailableContainers = containers,
            AvailableSubtitles = subtitles,
            AvailableAudioTracks = audioTracks
        };
    }
}