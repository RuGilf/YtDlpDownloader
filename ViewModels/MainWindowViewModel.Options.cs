using System.Collections.Generic;
using System.Collections.ObjectModel;
using YtDlpDownloader.Models;

namespace YtDlpDownloader.ViewModels;

public partial class MainWindowViewModel
{
    private void ApplyVideoOptions(VideoInfo video)
    {
        ReplaceItems(Resolutions, video.AvailableResolutions);
        SelectedResolution = Resolutions.Count > 0 ? Resolutions[0] : "Максимальное";

        ReplaceItems(Containers, video.AvailableContainers);
        SelectedContainer = Containers.Count > 0 ? Containers[0] : "MP4";

        ReplaceItems(Subtitles, video.AvailableSubtitles);
        SelectedSubtitle = "Без субтитров";

        ReplaceItems(AudioTracks, video.AvailableAudioTracks);
        SelectedAudioTrack = "Оригинал";
    }

    private DownloadOptions CreateDownloadOptions()
    {
        return new DownloadOptions
        {
            Resolution = SelectedResolution,
            Container = SelectedContainer,
            SelectedSubtitle = SelectedSubtitle,
            SelectedAudioTrack = SelectedAudioTrack,
            EmbedThumbnail = EmbedThumbnail,
            EmbedMetadata = EmbedMetadata
        };
    }

    private void ResetCurrentVideo()
    {
        VideoUrl = string.Empty;
        CurrentVideo = null;
        PreviewImage = null;
    }

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
