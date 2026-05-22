using System.Text.Json.Serialization;

namespace YtDlpDownloader.Services.Implementations;

internal class YtDlpVideoResponse
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("uploader")]
    public string Uploader { get; set; } = string.Empty;

    [JsonPropertyName("duration_string")]
    public string DurationString { get; set; } = string.Empty;

    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; } = string.Empty;

    [JsonPropertyName("formats")]
    public YtDlpFormatResponse[] Formats { get; set; } = [];
}

internal class YtDlpFormatResponse
{
    [JsonPropertyName("format_id")]
    public string FormatId { get; set; } = string.Empty;

    [JsonPropertyName("ext")]
    public string Ext { get; set; } = string.Empty;

    [JsonPropertyName("resolution")]
    public string Resolution { get; set; } = string.Empty;

    [JsonPropertyName("filesize")]
    public long? Filesize { get; set; }

    [JsonPropertyName("filesize_approx")]
    public long? FilesizeApprox { get; set; }
}