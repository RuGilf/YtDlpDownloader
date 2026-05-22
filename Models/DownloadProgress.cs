namespace YtDlpDownloader.Models;

public class DownloadProgress
{
    public double Percentage { get; set; }
    public string Speed { get; set; } = string.Empty;
    public string RemainingTime { get; set; } = string.Empty;
}