using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using YtDlpDownloader.Models;
using YtDlpDownloader.Services.Interfaces;

namespace YtDlpDownloader.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IYtDlpService _ytDlpService;
    private readonly IDependencyManager _dependencyManager;
    private readonly IFileDialogService _fileDialogService;
    private readonly IDownloadQueueManager _downloadQueueManager;
    private readonly IImageLoader _imageLoader;
    private readonly IDownloadTaskFactory _downloadTaskFactory;
    private CancellationTokenSource? _analysisCts;

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
        IDownloadQueueManager downloadQueueManager,
        IImageLoader imageLoader,
        IDownloadTaskFactory downloadTaskFactory)
    {
        _ytDlpService = ytDlpService;
        _dependencyManager = dependencyManager;
        _fileDialogService = fileDialogService;
        _downloadQueueManager = downloadQueueManager;
        _imageLoader = imageLoader;
        _downloadTaskFactory = downloadTaskFactory;
        QueueTasks.CollectionChanged += OnQueueTasksChanged;

        DownloadFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        _ = InitializeAsync();
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

}
