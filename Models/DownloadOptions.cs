namespace YtDlpDownloader.Models;

public class DownloadOptions
{
    public string Resolution { get; init; } = "1080p";
    public string Container { get; init; } = "MP4";
    public string SelectedSubtitle { get; init; } = "Без субтитров";
    public string SelectedAudioTrack { get; init; } = "Оригинал";
    
    public bool EmbedThumbnail { get; init; }
    public bool EmbedMetadata { get; init; }
}