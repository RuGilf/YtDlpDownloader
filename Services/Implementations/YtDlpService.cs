using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YtDlpDownloader.Models;
using YtDlpDownloader.Services.Interfaces;

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

    public async Task<VideoInfo> GetVideoInfoAsync(string url)
    {
        var binary = GetBinaryName();
        var args = $"--dump-json \"{url}\"";
        var jsonResult = await _processRunner.RunAndGetOutputAsync(binary, args);

        var rawData = JsonSerializer.Deserialize<YtDlpVideoResponse>(jsonResult);
        if (rawData == null)
            throw new Exception("Не удалось получить информацию о видео. Неверный формат данных");
        
        var videoInfo = new VideoInfo
        {
            Title = rawData.Title,
            Author = rawData.Uploader,
            Duration = rawData.DurationString,
            ThumbnailUrl = rawData.Thumbnail,
            AvailableFormats = rawData.Formats
            .Where(f => !string.IsNullOrEmpty(f.Resolution) && f.Resolution != "audio only")
            .Select(f => new VideoFormat
            {
                FormatId = f.FormatId,
                Extension = f.Ext,
                Resolution = f.Resolution,
                FileSize = FormatBytes(f.Filesize ?? f.FilesizeApprox)
            }).ToList()
        };

        return videoInfo;
    }

    public async Task DownloadVideoAsync(string url, VideoFormat format, string outputFolder, IProgress<DownloadProgress> progress)
    {
        var binary = GetBinaryName();

        var outputPath = Path.Combine(outputFolder, "%(title)s.%(ext)s");
        var args = $"-f \"{format.FormatId}+bestaudio/best\" -o \"{outputPath}\" \"{url}\"";

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

    private static string FormatBytes(long? bytes)
    {
        if (bytes == null) return "Размер неизвестен";

        string[] suffix = { "B", "KB", "MB", "GB", "TB"};
        double dblSbyte = bytes.Value;
        int i;
        for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            dblSbyte = bytes.Value / 1024.0;
        
        return $"{dblSbyte:0.##} {suffix[i]}";
    }
}