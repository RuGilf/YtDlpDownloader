using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using YtDlpDownloader.Models;
using YtDlpDownloader.Services.Interfaces;

namespace YtDlpDownloader.Services.Implementations;

public class DownloadQueueManager : IDownloadQueueManager
{
    private readonly IYtDlpService _ytDlpService;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isProcessing;
    private CancellationTokenSource? _cts;

    public ObservableCollection<DownloadTask> Tasks { get; } = new();

    public DownloadQueueManager(IYtDlpService ytDlpService)
    {
        _ytDlpService = ytDlpService;
    }

    public void AddTask(DownloadTask task)
    {
        Tasks.Add(task);
    }

    public void RemoveTask(DownloadTask task)
    {
        Tasks.Remove(task);
    }

    public void StopQueue()
    {
        _cts?.Cancel();
    }

    public async Task StartQueueAsync()
    {
        if (_isProcessing) return;
        _isProcessing = true;
        _cts = new CancellationTokenSource();

        await Task.Run(async () =>
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    DownloadTask? task = null;

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        task = System.Linq.Enumerable.FirstOrDefault(Tasks, t => t.Status == DownloadStatus.Pending);
                    });

                    if (task == null) break;

                    await _semaphore.WaitAsync(_cts.Token);
                    try
                    {
                        task.Status = DownloadStatus.Downloading;

                        var progressReporter = new Progress<DownloadProgress>(p =>
                        {
                            task.Progress = p.Percentage;
                            task.Speed = p.Speed;
                            task.RemainingTime = p.RemainingTime;
                        });

                        await _ytDlpService.DownloadVideoAsync(
                            task.Url, 
                            task.Options, 
                            task.DownloadFolderPath, 
                            progressReporter,
                            _cts.Token); 

                        task.Status = DownloadStatus.Completed;
                    }
                    catch (Exception)
                    {
                        task.Status = _cts.Token.IsCancellationRequested 
                            ? DownloadStatus.Pending 
                            : DownloadStatus.Failed;
                        
                        task.Progress = 0;
                        task.Speed = string.Empty;
                        task.RemainingTime = string.Empty;
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
            }
            finally
            {
                _isProcessing = false;
                _cts?.Dispose();
                _cts = null;
            }
        });
    }
}