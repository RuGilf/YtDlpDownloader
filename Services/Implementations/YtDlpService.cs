using System;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YtDlpDownloader.Models;
using YtDlpDownloader.Services.Interfaces;
using YtDlpDownloader.Services.Parser;

namespace YtDlpDownloader.Services.Implementations;

public class YtDlpService : IYtDlpService
{
    private readonly IProcessRunner _processRunner;

    private static readonly Regex ProgressRegex = new Regex(
        @"\[download\]\s+(?<percent>[0-9.]+)%\s+of\s+(?<size>.+?)\s+at\s+(?<speed>.+?)\s+ETA\s+(?<eta>.+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public YtDlpService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    private string GetBinaryName()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "yt-dlp.exe" : "yt-dlp";
    }

    private string ExtractSingleJson(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return string.Empty;

        var lines = output.Split(new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
                return trimmed;
        }

        return output;
    }

    private bool IsPlaylistUrl(string url)
    {
        return url.Contains("list=") || url.Contains("playlist") || url.Contains("/sets/");
    }

    public async Task<VideoInfo> GetVideoInfoAsync(string url)
    {
        var binary = GetBinaryName();

        var args = IsPlaylistUrl(url)
            ? $"--flat-playlist --dump-single-json \"{url}\""
            : $"--dump-json \"{url}\"";

        var jsonResult = await _processRunner.RunAndGetOutputAsync(binary, args);
        var cleanedJson = ExtractSingleJson(jsonResult);

        using var doc = JsonDocument.Parse(cleanedJson);
        var root = doc.RootElement;

        var parserFactory = new MetadataParserFactory();
        var parser = parserFactory.GetParser(root);

        return parser.Parse(root);
    }

    public async Task DownloadVideoAsync(string url, VideoFormat format, string outputFolder, IProgress<DownloadProgress> progress)
    {
        var binary = GetBinaryName();
        var outputPath = Path.Combine(outputFolder, "%(title)s.%(ext)s");

        var formatArgs = format.FormatId == "best" ? "bestvideo+bestaudio/best"
            : format.FormatId;
        var args = $"-f \"{formatArgs}\" -P \"{outputFolder}\" -o \"%(title)s.%(ext)s\" \"{url}\"";

        await _processRunner.RunAndReadAsync(binary, args, line =>
        {
            var match = ProgressRegex.Match(line);
            if (match.Success)
            {
                double.TryParse(match.Groups["percent"].Value, out var percent);
                var speed = match.Groups["speed"].Value;
                var eta = match.Groups["eta"].Value;

                progress.Report(new DownloadProgress
                {
                    Percentage = percent,
                    Speed = speed,
                    RemainingTime = eta
                });
            }
        });
    }
}