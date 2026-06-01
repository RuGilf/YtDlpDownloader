using System.Collections.Specialized;
using YtDlpDownloader.Models;

namespace YtDlpDownloader.ViewModels;

public partial class MainWindowViewModel
{
    public bool IsQueueEmpty => QueueTasks.Count == 0;
    public bool HasQueueTasks => QueueTasks.Count > 0;
    public bool IsNotDownloading => !IsDownloading;
    public bool CanEditRequest => !IsLoading && !IsDownloading;
    public bool CanAnalyze => CanEditRequest && !DependenciesMissing && !string.IsNullOrWhiteSpace(VideoUrl);
    public bool CanStartDownload => HasQueueTasks && !IsDownloading && !DependenciesMissing;
    public bool CanClearQueue => HasQueueTasks && !IsDownloading;
    public bool CanChangeFolder => !IsDownloading;
    public bool CanAddCurrentVideo => CurrentVideo != null && !IsDownloading && !DependenciesMissing;

    private void OnQueueTasksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsQueueEmpty));
        OnPropertyChanged(nameof(HasQueueTasks));
        OnPropertyChanged(nameof(CanStartDownload));
        OnPropertyChanged(nameof(CanClearQueue));
    }

    partial void OnVideoUrlChanged(string value)
    {
        OnPropertyChanged(nameof(CanAnalyze));
    }

    partial void OnIsLoadingChanged(bool value)
    {
        NotifyInteractionStateChanged();
    }

    partial void OnIsDownloadingChanged(bool value)
    {
        NotifyInteractionStateChanged();
    }

    partial void OnDependenciesMissingChanged(bool value)
    {
        NotifyInteractionStateChanged();
    }

    partial void OnCurrentVideoChanged(VideoInfo? value)
    {
        OnPropertyChanged(nameof(CanAddCurrentVideo));
    }

    private void NotifyInteractionStateChanged()
    {
        OnPropertyChanged(nameof(CanEditRequest));
        OnPropertyChanged(nameof(IsNotDownloading));
        OnPropertyChanged(nameof(CanAnalyze));
        OnPropertyChanged(nameof(CanStartDownload));
        OnPropertyChanged(nameof(CanClearQueue));
        OnPropertyChanged(nameof(CanChangeFolder));
        OnPropertyChanged(nameof(CanAddCurrentVideo));
    }
}
