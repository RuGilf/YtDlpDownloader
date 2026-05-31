using System.Text.Json;
using YtDlpDownloader.Models;

namespace YtDlpDownloader.Services.Parser;

public interface IMetadataParser
{
    VideoInfo Parse(JsonElement root);
}