using System;
using System.Threading.Tasks;

namespace YtDlpDownloader.Services.Interfaces;

public interface IDependencyManager
{
    Task<bool> CheckDependenciesExistAsync();
    Task DownloadDependenciesAsync(IProgress<double> downloadProgress);
    Task UpdateYtDlpAsync();
}