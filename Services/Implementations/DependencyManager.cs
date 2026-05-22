using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "yt-dlp.exe" : "yt-dlp";
    
    private string GetFfmpegBinary() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
    
    public async Task<bool> CheckDependenciesExistAsync()
    {
        bool ytDlpExists = await CheckBinaryExistsAsync(GetYtDlpBinary(), "--version");
        bool ffmpegExists = await CheckBinaryExistsAsync(GetFfmpegBinary(), "-version");

        return ytDlpExists && ffmpegExists;
    }

    public async Task DownloadDependenciesAsync(IProgress<double> downloadProgress)
    {
        await Task.CompletedTask;
    }

    public async Task UpdateYtDlpAsync()
    {
        var binary = GetYtDlpBinary();
        await _processRunner.RunAndGetOutputAsync(binary, "--update");
    }

    private async Task<bool> CheckBinaryExistsAsync(string binary, string testArgs)
    {
        try
        {
            await _processRunner.RunAndGetOutputAsync(binary, testArgs);
            return true;
        }
        catch
        {
            return false;
        }
    }
}