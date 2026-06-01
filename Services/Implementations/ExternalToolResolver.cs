using System;
using System.IO;
using System.Runtime.InteropServices;

namespace YtDlpDownloader.Services.Implementations;

internal static class ExternalToolResolver
{
    public static string GetYtDlpBinaryName() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "yt-dlp.exe" : "yt-dlp";

    public static string GetFfmpegBinaryName() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";

    public static string ResolveExecutable(string binaryName)
    {
        if (Path.IsPathFullyQualified(binaryName))
        {
            if (File.Exists(binaryName))
            {
                return binaryName;
            }

            throw new FileNotFoundException($"Не найден исполняемый файл: {binaryName}", binaryName);
        }

        foreach (var directory in GetSearchDirectories())
        {
            var candidate = Path.GetFullPath(Path.Combine(directory, binaryName));
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Не найден исполняемый файл: {binaryName}", binaryName);
    }

    private static string[] GetSearchDirectories()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathDirectories = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        var localToolsDirectory = Path.Combine(AppContext.BaseDirectory, "tools");
        var directories = new string[pathDirectories.Length + 2];
        directories[0] = localToolsDirectory;
        directories[1] = AppContext.BaseDirectory;

        Array.Copy(pathDirectories, 0, directories, 2, pathDirectories.Length);
        return directories;
    }
}
