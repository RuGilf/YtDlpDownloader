using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using YtDlpDownloader.Models;

namespace YtDlpDownloader.ViewModels;

public partial class MainWindowViewModel
{
    [RelayCommand]
    private async Task AnalyzeVideoAsync()
    {
        if (string.IsNullOrWhiteSpace(VideoUrl))
        {
            StatusText = "Пожалуйста, введите ссылку";
            return;
        }

        CancellationTokenSource? analysisCts = null;

        try
        {
            _analysisCts?.Cancel();
            analysisCts = new CancellationTokenSource();
            _analysisCts = analysisCts;

            IsLoading = true;
            StatusText = "Анализируем ссылку...";
            CurrentVideo = null;
            PreviewImage = null;

            var video = await _ytDlpService.GetVideoInfoAsync(VideoUrl, analysisCts.Token);
            CurrentVideo = video;
            ApplyVideoOptions(video);

            StatusText = "Загрузка превью...";
            if (!string.IsNullOrEmpty(video.ThumbnailUrl))
            {
                PreviewImage = await _imageLoader.LoadFromUrlAsync(video.ThumbnailUrl, analysisCts.Token);
            }

            StatusText = "Выберите настройки и добавьте в очередь";
        }
        catch (OperationCanceledException)
        {
            if (ReferenceEquals(_analysisCts, analysisCts))
            {
                StatusText = "Анализ видео отменён";
            }
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(_analysisCts, analysisCts))
            {
                StatusText = $"Ошибка анализа: {ex.Message}";
            }
        }
        finally
        {
            if (ReferenceEquals(_analysisCts, analysisCts))
            {
                IsLoading = false;
                analysisCts?.Dispose();
                _analysisCts = null;
            }
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

        var options = CreateDownloadOptions();
        var tasks = _downloadTaskFactory.CreateTasks(CurrentVideo, VideoUrl, options, DownloadFolderPath, PreviewImage);

        if (CurrentVideo.PlaylistEntries != null && CurrentVideo.PlaylistEntries.Count > 0)
        {
            foreach (var task in tasks) _downloadQueueManager.AddTask(task);
            StatusText = $"Добавлено в очередь {CurrentVideo.PlaylistEntries.Count} видео из плейлиста.";
        }
        else
        {
            var task = tasks[0];
            _downloadQueueManager.AddTask(task);
            StatusText = $"Добавлено в очередь: {task.Title} [{SelectedResolution} | {SelectedContainer}]";
        }

        ResetCurrentVideo();

        if (!IsDownloading)
        {
            _ = StartDownloadAsync();
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

            var completedSuccessfully = await _downloadQueueManager.StartQueueAsync();

            StatusText = completedSuccessfully
                ? "Все загрузки завершены!"
                : "Очередь завершена с ошибками или была остановлена";
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
                _downloadQueueManager.RemoveTask(QueueTasks[i]);
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
        _analysisCts?.Cancel();
        ResetCurrentVideo();
        StatusText = "Анализ видео отменён";
    }
}
