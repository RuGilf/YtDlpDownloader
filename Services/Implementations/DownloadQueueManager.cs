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
    private readonly object _stateLock = new();
    private bool _isProcessing;
    private CancellationTokenSource? _cts;

    public ObservableCollection<DownloadTask> Tasks { get; } = new();

    public DownloadQueueManager(IYtDlpService ytDlpService)
    {
        _ytDlpService = ytDlpService;
    }

    public void AddTask(DownloadTask task)
    {
        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            Tasks.Add(task);
            return;
        }

        Avalonia.Threading.Dispatcher.UIThread.Post(() => Tasks.Add(task));
    }

    public void RemoveTask(DownloadTask task)
    {
        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            Tasks.Remove(task);
            return;
        }

        Avalonia.Threading.Dispatcher.UIThread.Post(() => Tasks.Remove(task));
    }

    public void StopQueue()
    {
        lock (_stateLock)
        {
            _cts?.Cancel();
        }
    }

    public async Task<bool> StartQueueAsync()
    {
        CancellationTokenSource cts;

        lock (_stateLock)
        {
            if (_isProcessing) return false;

            _isProcessing = true;
            _cts = new CancellationTokenSource();
            cts = _cts;
        }

        var completedSuccessfully = true;

        try
        {
            await Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    DownloadTask? task = null;

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        task = System.Linq.Enumerable.FirstOrDefault(Tasks, t => t.Status == DownloadStatus.Pending);
                    });

                    if (task == null) break;

                    await _semaphore.WaitAsync(cts.Token);
                    try
                    {
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            task.Status = DownloadStatus.Downloading;
                        });

                        var progressReporter = new Progress<DownloadProgress>(p =>
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                task.Progress = p.Percentage;
                                task.Speed = p.Speed;
                                task.RemainingTime = p.RemainingTime;
                            });
                        });

                        await _ytDlpService.DownloadVideoAsync(
                            task.Url, 
                            task.Options, 
                            task.DownloadFolderPath, 
                            progressReporter,
                            cts.Token); 

                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            task.Status = DownloadStatus.Completed;
                        });
                    }
                    catch (Exception)
                    {
                        completedSuccessfully = false;

                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            task.Status = cts.Token.IsCancellationRequested
                                ? DownloadStatus.Pending
                                : DownloadStatus.Failed;

                            task.Progress = 0;
                            task.Speed = string.Empty;
                            task.RemainingTime = string.Empty;
                        });
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
            }, cts.Token);

            return completedSuccessfully && !cts.Token.IsCancellationRequested;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            lock (_stateLock)
            {
                if (ReferenceEquals(_cts, cts))
                {
                    _isProcessing = false;
                    _cts.Dispose();
                    _cts = null;
                }
            }
        }
    }
}
