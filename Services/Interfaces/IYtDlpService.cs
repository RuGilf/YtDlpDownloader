using System;
using System.Threading.Tasks;
using YtDlpDownloader.Models;

namespace YtDlpDownloader.Services.Interfaces;

public interface IYtDlpService
{
    Task<VideoInfo> GetVideoInfoAsync(string url);
    Task DownloadVideoAsync(string url, VideoFormat format, string outputFolder, IProgress<DownloadProgress> progress);
}