using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    private static readonly Regex LanguageCodeRegex = new(
        @"^[A-Za-z0-9_-]+$",
        RegexOptions.Compiled);

    private static readonly HashSet<string> AllowedContainers = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp4",
        "mkv",
        "webm",
        "mp3"
    };

    public YtDlpService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    private string GetBinaryName()
    {
        return ExternalToolResolver.ResolveExecutable(ExternalToolResolver.GetYtDlpBinaryName());
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

    public async Task<VideoInfo> GetVideoInfoAsync(string url, CancellationToken cancellationToken = default)
    {
        ValidateUrl(url);

        var binary = GetBinaryName();
        
        var args = IsPlaylistUrl(url)
            ? new List<string> { "--flat-playlist", "--dump-single-json", url }
            : new List<string> { "--dump-json", url };
        
        var jsonResult = await _processRunner.RunAndGetOutputAsync(binary, args, cancellationToken);
        var cleanedJson = ExtractSingleJson(jsonResult);

        using var doc = JsonDocument.Parse(cleanedJson);
        var root = doc.RootElement;

        var parserFactory = new MetadataParserFactory();
        var parser = parserFactory.GetParser(root);

        return parser.Parse(root);
    }

    public async Task DownloadVideoAsync(string url, DownloadOptions options, string outputFolder, IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
    {
        ValidateUrl(url);

        var binary = GetBinaryName();
        var args = BuildArguments(url, options, outputFolder);

        await _processRunner.RunAndReadAsync(binary, args, line =>
        {
            var match = ProgressRegex.Match(line);
            if (match.Success)
            {
                double.TryParse(match.Groups["percent"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent);
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

    private IReadOnlyList<string> BuildArguments(string url, DownloadOptions options, string outputFolder)
    {
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            throw new ArgumentException("Папка сохранения не выбрана.", nameof(outputFolder));
        }

        var fullOutputFolder = Path.GetFullPath(outputFolder);
        Directory.CreateDirectory(fullOutputFolder);

        var extraFlags = new List<string>();
        var container = NormalizeContainer(options.Container);
        var height = NormalizeResolution(options.Resolution);

        var audioFilter = string.Equals(options.SelectedAudioTrack, "Оригинал", StringComparison.Ordinal)
            ? "bestaudio"
            : $"bestaudio[language={NormalizeLanguageCode(options.SelectedAudioTrack, "аудиодорожка")}]";

        string formatArg;
        if (container == "mp3")
        {
            formatArg = audioFilter;
            extraFlags.Add("-x");
            extraFlags.Add("--audio-format");
            extraFlags.Add("mp3");
        }
        else
        {
            if (container == "mp4")
            {
                formatArg = string.IsNullOrEmpty(height)
                    ? $"bestvideo[ext=mp4]+{audioFilter}[ext=m4a]/best[ext=mp4]/best"
                    : $"bestvideo[height<={height}][ext=mp4]+{audioFilter}[ext=m4a]/best[height<={height}]";

                extraFlags.Add("--merge-output-format");
                extraFlags.Add("mp4");
            }
            else
            {
                formatArg = string.IsNullOrEmpty(height)
                    ? $"bestvideo+{audioFilter}/best"
                    : $"bestvideo[height<={height}]+{audioFilter}/best[height<={height}]";

                extraFlags.Add("--merge-output-format");
                extraFlags.Add(container);
            }
        }

        if (!string.Equals(options.SelectedSubtitle, "Без субтитров", StringComparison.Ordinal))
        {
            bool isAuto = options.SelectedSubtitle.EndsWith(" (авто)");
            var langCode = NormalizeLanguageCode(options.SelectedSubtitle.Replace(" (авто)", "").Trim(), "субтитры");

            if (isAuto)
            {
                extraFlags.Add("--write-auto-subs");
                extraFlags.Add("--sub-langs");
                extraFlags.Add(langCode);
            }
            else
            {
                extraFlags.Add("--write-subs");
                extraFlags.Add("--sub-langs");
                extraFlags.Add(langCode);
            }
        }

        if (options.EmbedThumbnail && container != "mp3")
        {
            extraFlags.Add("--embed-thumbnail");
        }

        if (options.EmbedMetadata)
        {
            extraFlags.Add("--embed-metadata");
        }

        var args = new List<string>
        {
            "-f",
            formatArg
        };
        args.AddRange(extraFlags);
        args.Add("-P");
        args.Add(fullOutputFolder);
        args.Add("-o");
        args.Add("%(title)s.%(ext)s");
        args.Add(url);

        return args;
    }

    private static void ValidateUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Введите корректную HTTP/HTTPS-ссылку.", nameof(url));
        }
    }

    private static string NormalizeContainer(string container)
    {
        var normalized = container.Trim().ToLowerInvariant();
        if (!AllowedContainers.Contains(normalized))
        {
            throw new ArgumentException($"Неподдерживаемый формат файла: {container}");
        }

        return normalized;
    }

    private static string NormalizeResolution(string resolution)
    {
        if (string.Equals(resolution, "Максимальное", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var normalized = resolution.Trim().ToLowerInvariant();
        if (!normalized.EndsWith('p') ||
            !int.TryParse(normalized[..^1], NumberStyles.None, CultureInfo.InvariantCulture, out var height) ||
            height <= 0)
        {
            throw new ArgumentException($"Неподдерживаемое разрешение: {resolution}");
        }

        return height.ToString(CultureInfo.InvariantCulture);
    }

    private static string NormalizeLanguageCode(string languageCode, string optionName)
    {
        if (string.IsNullOrWhiteSpace(languageCode) || !LanguageCodeRegex.IsMatch(languageCode))
        {
            throw new ArgumentException($"Некорректное значение параметра \"{optionName}\": {languageCode}");
        }

        return languageCode;
    }
}
