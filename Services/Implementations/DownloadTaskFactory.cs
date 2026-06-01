using System.Collections.Generic;
using Avalonia.Media.Imaging;
using YtDlpDownloader.Models;
using YtDlpDownloader.Services.Interfaces;

namespace YtDlpDownloader.Services.Implementations;

public class DownloadTaskFactory : IDownloadTaskFactory
{
    public IReadOnlyList<DownloadTask> CreateTasks(VideoInfo video, string sourceUrl, DownloadOptions options, string downloadFolderPath, Bitmap? previewImage)
    {
        if (video.PlaylistEntries is { Count: > 0 })
        {
            var playlistTasks = new List<DownloadTask>(video.PlaylistEntries.Count);
            foreach (var entry in video.PlaylistEntries)
            {
                playlistTasks.Add(new DownloadTask
                {
                    Title = entry.Title,
                    Author = entry.Author,
                    Url = entry.Url,
                    Options = options,
                    DownloadFolderPath = downloadFolderPath,
                    PreviewImage = null
                });
            }

            return playlistTasks;
        }

        return
        [
            new DownloadTask
            {
                Title = video.Title,
                Author = video.Author,
                Url = sourceUrl,
                Options = options,
                DownloadFolderPath = downloadFolderPath,
                PreviewImage = previewImage
            }
        ];
    }
}
