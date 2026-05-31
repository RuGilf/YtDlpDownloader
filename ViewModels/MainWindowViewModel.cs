using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
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
    [ObservableProperty] private VideoFormat? _selectedFormat;
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private string _statusText = "Готов к работе";
    [ObservableProperty] private bool _dependenciesMissing;

    [ObservableProperty] private string _downloadFolderPath = string.Empty;
    [ObservableProperty] private Bitmap? _previewImage;

    public ObservableCollection<DownloadTask> QueueTasks => _downloadQueueManager.Tasks;

    public MainWindowViewModel(IYtDlpService ytDlpService, IDependencyManager dependencyManager, IFileDialogService fileDialogService, IDownloadQueueManager downloadQueueManager)
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
            StatusText = "Ошибка: установите yt-dlp и ffmpeg в вашей системе!";
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
            StatusText = "Анализирую видео...";
            CurrentVideo = null;
            PreviewImage = null;

            var video = await _ytDlpService.GetVideoInfoAsync(VideoUrl);
            CurrentVideo = video;
            StatusText = "Загрузка превью...";

            if (!string.IsNullOrEmpty(video.ThumbnailUrl))
                PreviewImage = await ImageLoader.LoadFromUrlAsync(video.ThumbnailUrl);

            StatusText = "Видео добавлено в очередь";
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
            StatusText = $"Папка сохранения изменена на {Path.GetFileName(selectedFolder)}";
        }
    }

    [RelayCommand]
    private async Task DownloadVideoAsync()
    {
        if (CurrentVideo == null || SelectedFormat == null)
        {
            StatusText = "Выберите видео и формат для скачивания";
            return;
        }

        try
        {
            IsDownloading = true;
            DownloadProgress = 0;
            StatusText = "Подготовка к скачиванию...";

            var downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), DownloadFolderPath);

            var progressReporter = new Progress<DownloadProgress>(p =>
            {
                DownloadProgress = p.Percentage;
                StatusText = $"Скачивание: {p.Percentage}% | Скорость: {p.Speed} | Осталось: {p.RemainingTime}";
            });

            await _ytDlpService.DownloadVideoAsync(VideoUrl, SelectedFormat, downloadsFolder, progressReporter);
            StatusText = "Скачивание завершено! Файл сохранён в папку Загрузки";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка скачивания: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
        }
    }

    [RelayCommand]
    private void AddToQueue()
    {
        if (CurrentVideo == null || SelectedFormat == null)
            return;

        var task = new DownloadTask
        {
            Title = CurrentVideo.Title,
            Author = CurrentVideo.Author,
            Url = VideoUrl,
            SelectedFormat = SelectedFormat,
            DownloadFolderPath = DownloadFolderPath,
            PreviewImage = PreviewImage
        };

        _downloadQueueManager.AddTask(task);

        VideoUrl = string.Empty;
        CurrentVideo = null;
        PreviewImage = null;
        SelectedFormat = null;

        StatusText = $"Добавлено в очередь: {task.Title}";
    }

    [RelayCommand]
    private async Task StartDownload()
    {
        if (QueueTasks.Count == 0)
        {
            StatusText = "Очередь загрузок пуста!";
            return;
        }

        try
        {
            IsDownloading = true;
            StatusText = "Запуск очереди загрузок...";

            await _downloadQueueManager.StartQueueAsync();

            StatusText = "Все загрузки из очереди завершены";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка обработки очереди {ex.Message}";
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
            if (QueueTasks[i].Status != DownloadStatus.Downloading)
                _downloadQueueManager.Tasks.RemoveAt(i);
        StatusText = "Очередь очищена от неактивных задач";
    }

    [RelayCommand]
    private void RemoveTask(DownloadTask task)
    {
        _downloadQueueManager.RemoveTask(task);
        StatusText = "Видео удалено из очереди";
    }
}
