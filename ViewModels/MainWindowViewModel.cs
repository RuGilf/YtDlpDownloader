using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpDownloader.Models;
using YtDlpDownloader.Services;
using YtDlpDownloader.Services.Implementations;
using YtDlpDownloader.Services.Interfaces;

namespace YtDlpDownloader.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IYtDlpService _ytDlpService;
    private readonly IDependencyManager _dependencyManager;
    private readonly IFileDialogService _fileDialogService;
    private readonly IDownloadQueueManager _downloadQueueManager;

    [ObservableProperty] private string _videoUrl = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private VideoInfo? _currentVideo;
    [ObservableProperty] private string _statusText = "Готов к работе";
    [ObservableProperty] private bool _dependenciesMissing;
    [ObservableProperty] private string _downloadFolderPath = string.Empty;
    [ObservableProperty] private Bitmap? _previewImage;

    public ObservableCollection<string> Resolutions { get; } = new();
    public ObservableCollection<string> Containers { get; } = new();
    public ObservableCollection<string> Subtitles { get; } = new();
    public ObservableCollection<string> AudioTracks { get; } = new();

    [ObservableProperty] private string _selectedResolution = "1080p";
    [ObservableProperty] private string _selectedContainer = "MP4";
    [ObservableProperty] private string _selectedSubtitle = "Без субтитров";
    [ObservableProperty] private string _selectedAudioTrack = "Оригинал";
    [ObservableProperty] private bool _embedThumbnail = true;
    [ObservableProperty] private bool _embedMetadata = true;

    public ObservableCollection<DownloadTask> QueueTasks => _downloadQueueManager.Tasks;

    public MainWindowViewModel(
        IYtDlpService ytDlpService, 
        IDependencyManager dependencyManager,
        IFileDialogService fileDialogService,
        IDownloadQueueManager downloadQueueManager)
    {
        _ytDlpService = ytDlpService;
        _dependencyManager = dependencyManager;
        _fileDialogService = fileDialogService;
        _downloadQueueManager = downloadQueueManager;

        DownloadFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        Task.Run(InitializeAsync);
    }

    private async Task InitializeAsync()
    {
        var exist = await _dependencyManager.CheckDependenciesExistAsync();
        if (!exist)
        {
            DependenciesMissing = true;
            StatusText = "Ошибка: Установите yt-dlp и ffmpeg в вашей системе!";
        }
    }

    [RelayCommand]
    private async Task AnalyzeVideoAsync()
    {
        if (string.IsNullOrWhiteSpace(VideoUrl))
        {
            StatusText = "Пожалуйста, введите ссылку";
            return;
        }

        try
        {
            IsLoading = true;
            StatusText = "Анализируем ссылку...";
            CurrentVideo = null;
            PreviewImage = null;

            var video = await _ytDlpService.GetVideoInfoAsync(VideoUrl);
            CurrentVideo = video;

            Resolutions.Clear();
            foreach (var r in video.AvailableResolutions) Resolutions.Add(r);
            SelectedResolution = Resolutions.Count > 0 ? Resolutions[0] : "Максимальное";

            Containers.Clear();
            foreach (var c in video.AvailableContainers) Containers.Add(c);
            SelectedContainer = Containers.Count > 0 ? Containers[0] : "MP4";

            Subtitles.Clear();
            foreach (var s in video.AvailableSubtitles) Subtitles.Add(s);
            SelectedSubtitle = "Без субтитров";

            AudioTracks.Clear();
            foreach (var a in video.AvailableAudioTracks) AudioTracks.Add(a);
            SelectedAudioTrack = "Оригинал";

            StatusText = "Загрузка превью...";
            if (!string.IsNullOrEmpty(video.ThumbnailUrl))
            {
                PreviewImage = await ImageLoader.LoadFromUrlAsync(video.ThumbnailUrl);
            }

            StatusText = "Выберите настройки и добавьте в очередь";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка анализа: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ChangeFolderAsync()
    {
        var selectedFolder = await _fileDialogService.SelectFolderAsync();
        if (!string.IsNullOrEmpty(selectedFolder))
        {
            DownloadFolderPath = selectedFolder;
            StatusText = $"Папка сохранения: {Path.GetFileName(selectedFolder)}";
        }
    }

    [RelayCommand]
    private void AddToQueue()
    {
        if (CurrentVideo == null) return;

        var options = new DownloadOptions
        {
            Resolution = SelectedResolution,
            Container = SelectedContainer,
            SelectedSubtitle = SelectedSubtitle,
            SelectedAudioTrack = SelectedAudioTrack,
            EmbedThumbnail = EmbedThumbnail,
            EmbedMetadata = EmbedMetadata
        };

        if (CurrentVideo.PlaylistEntries != null && CurrentVideo.PlaylistEntries.Count > 0)
        {
            foreach (var entry in CurrentVideo.PlaylistEntries)
            {
                var task = new DownloadTask
                {
                    Title = entry.Title,
                    Author = entry.Author,
                    Url = entry.Url,
                    Options = options, 
                    DownloadFolderPath = DownloadFolderPath,
                    PreviewImage = null
                };
                _downloadQueueManager.AddTask(task);
            }
            StatusText = $"Добавлено в очередь {CurrentVideo.PlaylistEntries.Count} видео из плейлиста.";
        }
        else
        {
            var task = new DownloadTask
            {
                Title = CurrentVideo.Title,
                Author = CurrentVideo.Author,
                Url = VideoUrl,
                Options = options, 
                DownloadFolderPath = DownloadFolderPath,
                PreviewImage = PreviewImage
            };
            _downloadQueueManager.AddTask(task);
            StatusText = $"Добавлено в очередь: {task.Title} [{SelectedResolution} | {SelectedContainer}]";
        }

        VideoUrl = string.Empty;
        CurrentVideo = null;
        PreviewImage = null;

        if (!IsDownloading)
        {
            Task.Run(async () => await StartDownloadAsync());
        }
    }

    [RelayCommand]
    private async Task StartDownloadAsync()
    {
        if (QueueTasks.Count == 0) return;

        try
        {
            IsDownloading = true;
            StatusText = "Выполняется скачивание очереди...";
            
            await _downloadQueueManager.StartQueueAsync();

            StatusText = "Все загрузки завершены!";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка очереди: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
        }
    }

    [RelayCommand]
    private void ClearQueue()
    {
        for (int i = QueueTasks.Count - 1; i >= 0; i--)
        {
            if (QueueTasks[i].Status != DownloadStatus.Downloading)
            {
                _downloadQueueManager.Tasks.RemoveAt(i);
            }
        }
        StatusText = "Очередь очищена от неактивных задач";
    }

    [RelayCommand]
    private void RemoveTask(DownloadTask task)
    {
        _downloadQueueManager.RemoveTask(task);
        StatusText = "Задача удалена из очереди";
    }

    [RelayCommand]
    private void StopDownload()
    {
        _downloadQueueManager.StopQueue();
        StatusText = "Скачивание очереди остановлено пользователем";
    }

    [RelayCommand]
    private void CancelAnalysis()
    {
        CurrentVideo = null;
        VideoUrl = string.Empty;
        PreviewImage = null;
        StatusText = "Анализ видео отменён";
    }
}