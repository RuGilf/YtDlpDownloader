namespace YtDlpDownloader.Models;

public class DownloadOptions
{
    public string Resolution { get; init; } = "1080p";
    public string Container { get; init; } = "MP4";
    public bool DownloadSubtitles { get; init; }
    public string SubtitlesLanguage { get; init; } = "ru";
    public bool EmbedThumbnail { get; init; }
    public bool EmbedMetadata { get; init; }
}