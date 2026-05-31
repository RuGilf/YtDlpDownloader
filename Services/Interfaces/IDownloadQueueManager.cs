using System.Collections.ObjectModel;
using System.Threading.Tasks;
using YtDlpDownloader.Models;

namespace YtDlpDownloader.Services.Interfaces;

public interface IDownloadQueueManager
{
    ObservableCollection<DownloadTask> Tasks { get; }

    void AddTask(DownloadTask task);
    void RemoveTask(DownloadTask task);
    void StopQueue();

    Task StartQueueAsync();
}