using System;
using System.Collections.Generic;
using System.Text.Json;
using YtDlpDownloader.Models;

namespace YtDlpDownloader.Services.Parser;

public class PlaylistMetadataParser : IMetadataParser
{
    public VideoInfo Parse(JsonElement root)
    {
        var playlistTitle = root.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : "Плейлист";
        var playlistAuthor = root.TryGetProperty("uploader", out var uploaderProp) ? uploaderProp.GetString() : "Неизвестный канал";
        
        var entriesList = new List<PlaylistEntry>();
        
        if (root.TryGetProperty("entries", out var entriesProp) && entriesProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in entriesProp.EnumerateArray())
            {
                var entryTitle = entry.TryGetProperty("title", out var t) ? t.GetString() : "Без названия";
                var entryId = entry.TryGetProperty("id", out var idProp) ? idProp.GetString() : string.Empty;
                var entryAuthor = entry.TryGetProperty("uploader", out var u) ? u.GetString() : playlistAuthor;
                var entryUrl = GetEntryUrl(entry, entryId);

                entriesList.Add(new PlaylistEntry
                {
                    Title = entryTitle ?? "Без названия",
                    Url = entryUrl,
                    Author = entryAuthor ?? "Неизвестный автор"
                });
            }
        }

        return new VideoInfo
        {
            Title = $"[Плейлист] {playlistTitle}",
            Author = playlistAuthor ?? "Неизвестный автор",
            Duration = $"{entriesList.Count} видео",
            PlaylistEntries = entriesList,
            
            AvailableResolutions = new List<string> { "Максимальное" },
            AvailableContainers = new List<string> { "MP4", "MKV", "WebM", "MP3" },
            AvailableSubtitles = new List<string> { "Без субтитров", "ru", "en" },
            AvailableAudioTracks = new List<string> { "Оригинал" }
        };
    }

    private static string GetEntryUrl(JsonElement entry, string? entryId)
    {
        if (entry.TryGetProperty("webpage_url", out var webpageUrlProp))
        {
            var webpageUrl = webpageUrlProp.GetString();
            if (Uri.TryCreate(webpageUrl, UriKind.Absolute, out _))
            {
                return webpageUrl;
            }
        }

        if (entry.TryGetProperty("url", out var urlProp))
        {
            var url = urlProp.GetString();
            if (Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                return url;
            }
        }

        return string.IsNullOrWhiteSpace(entryId)
            ? string.Empty
            : $"https://www.youtube.com/watch?v={entryId}";
    }
}
