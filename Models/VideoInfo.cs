using System.Collections.Generic;

namespace YtDlpDownloader.Models;

public class PlaylistEntry
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
}

public class VideoInfo
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public List<VideoFormat> AvailableFormats { get; set; } = new();

    public List<PlaylistEntry>? PlaylistEntries { get; set; }
}