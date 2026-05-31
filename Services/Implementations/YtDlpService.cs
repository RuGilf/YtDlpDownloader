using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
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

        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            {
                return trimmed;
            }
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

    public async Task DownloadVideoAsync(string url, DownloadOptions options, string outputFolder, IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
    {
        var binary = GetBinaryName();
        var args = BuildArguments(url, options, outputFolder);

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
        }, cancellationToken);
    }

    private string BuildArguments(string url, DownloadOptions options, string outputFolder)
    {
        var formatArg = "";
        var extraFlags = "";
        var container = options.Container.ToLower();

        var audioFilter = options.SelectedAudioTrack == "Оригинал"
            ? "bestaudio"
            : $"bestaudio[language={options.SelectedAudioTrack}]";

        if (container == "mp3")
        {
            formatArg = audioFilter;
            extraFlags += " -x --audio-format mp3";
        }
        else
        {
            var height = options.Resolution switch
            {
                "1080p" => "1080",
                "720p" => "720",
                "480p" => "480",
                "360p" => "360",
                _ => string.Empty
            };

            if (container == "mp4")
            {
                formatArg = string.IsNullOrEmpty(height)
                    ? $"bestvideo[ext=mp4]+{audioFilter}[ext=m4a]/best[ext=mp4]/best"
                    : $"bestvideo[height<={height}][ext=mp4]+{audioFilter}[ext=m4a]/best[height<={height}]";

                extraFlags += " --merge-output-format mp4";
            }
            else
            {
                formatArg = string.IsNullOrEmpty(height)
                    ? $"bestvideo+{audioFilter}/best"
                    : $"bestvideo[height<={height}]+{audioFilter}/best[height<={height}]";

                extraFlags += $" --merge-output-format {container}";
            }
        }

        if (options.SelectedSubtitle != "Без субтитров")
        {
            bool isAuto = options.SelectedSubtitle.EndsWith(" (авто)");
            var langCode = options.SelectedSubtitle.Replace(" (авто)", "").Trim();

            if (isAuto)
            {
                extraFlags += $" --write-auto-subs --sub-langs \"{langCode}\"";
            }
            else
            {
                extraFlags += $" --write-subs --sub-langs \"{langCode}\"";
            }
        }

        if (options.EmbedThumbnail && container != "mp3")
        {
            extraFlags += " --embed-thumbnail";
        }

        if (options.EmbedMetadata)
        {
            extraFlags += " --embed-metadata";
        }

        return $"-f \"{formatArg}\" {extraFlags} -P \"{outputFolder}\" -o \"%(title)s.%(ext)s\" \"{url}\"";
    }
}