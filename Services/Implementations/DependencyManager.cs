using System;
using System.Threading.Tasks;
using YtDlpDownloader.Services.Interfaces;

namespace YtDlpDownloader.Services.Implementations;

public class DependencyManager : IDependencyManager
{
    private readonly IProcessRunner _processRunner;

    public DependencyManager(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    private string GetYtDlpBinary() =>
        ExternalToolResolver.ResolveExecutable(ExternalToolResolver.GetYtDlpBinaryName());
    
    private string GetFfmpegBinary() =>
        ExternalToolResolver.ResolveExecutable(ExternalToolResolver.GetFfmpegBinaryName());
    
    public async Task<bool> CheckDependenciesExistAsync()
    {
        bool ytDlpExists = await CheckBinaryExistsAsync(ExternalToolResolver.GetYtDlpBinaryName(), "--version");
        bool ffmpegExists = await CheckBinaryExistsAsync(ExternalToolResolver.GetFfmpegBinaryName(), "-version");

        return ytDlpExists && ffmpegExists;
    }

    public async Task DownloadDependenciesAsync(IProgress<double> downloadProgress)
    {
        await Task.FromException(new NotSupportedException("Автоматическая загрузка зависимостей не реализована. Установите yt-dlp и ffmpeg вручную или поместите их в папку tools рядом с приложением."));
    }

    public async Task UpdateYtDlpAsync()
    {
        var binary = GetYtDlpBinary();
        await _processRunner.RunAndGetOutputAsync(binary, ["--update"]);
    }

    private async Task<bool> CheckBinaryExistsAsync(string binaryName, string testArgs)
    {
        try
        {
            var binary = ExternalToolResolver.ResolveExecutable(binaryName);
            await _processRunner.RunAndGetOutputAsync(binary, [testArgs]);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
