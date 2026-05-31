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
                var entryUrl = $"https://www.youtube.com/watch?v={entryId}";

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
            AvailableFormats = new List<VideoFormat>
            {
                new() { FormatId = "best", Resolution = "Весь плейлист (Лучшее качество)", Extension = "mp4", FileSize = "Авто" }
            }
        };
    }
}