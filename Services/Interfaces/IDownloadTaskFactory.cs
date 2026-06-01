using System.Collections.Generic;
using Avalonia.Media.Imaging;
using YtDlpDownloader.Models;

namespace YtDlpDownloader.Services.Interfaces;

public interface IDownloadTaskFactory
{
    IReadOnlyList<DownloadTask> CreateTasks(VideoInfo video, string sourceUrl, DownloadOptions options, string downloadFolderPath, Bitmap? previewImage);
}
