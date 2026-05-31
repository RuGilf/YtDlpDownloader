using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YtDlpDownloader.Models;

public partial class DownloadTask : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public DownloadOptions Options { get; init; } = new();
    public string DownloadFolderPath { get; init; } = string.Empty;

    [ObservableProperty] private DownloadStatus _status = DownloadStatus.Pending;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _speed = string.Empty;
    [ObservableProperty] private string _remainingTime = string.Empty;
    [ObservableProperty] private Bitmap? _previewImage;
}