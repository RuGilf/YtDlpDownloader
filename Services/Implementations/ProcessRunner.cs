using System;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml.MarkupExtensions;
using YtDlpDownloader.Services.Interfaces;

namespace YtDlpDownloader.Services.Implementations;

public class ProcessRunner : IProcessRunner
{
    public async Task<string> RunAndGetOutputAsync(string fileName, string arguments)
    {
        var startInfo = CreateStartInfo(fileName, arguments);

        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                outputBuilder.AppendLine(args.Data);
        };
        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                errorBuilder.AppendLine(args.Data);
        };

        if (!process.Start())
            throw new InvalidOperationException($"Не удалось запустить процесс: {fileName}");

        process.StandardInput.Close();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new Exception($"Процесс завершился с ошибкой (код {process.ExitCode})\nДетали: {errorBuilder}");

        return outputBuilder.ToString();
    }

    // Чтение выввода построчно
    public async Task RunAndReadAsync(string fileName, string arguments, Action<string> onOutputReceived)
    {
        var startInfo = CreateStartInfo(fileName, arguments);

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                onOutputReceived(args.Data);
        };

        if (!process.Start())
            throw new InvalidOperationException($"Не удалось запустить процесс {fileName}");

        process.StandardInput.Close();

        process.BeginOutputReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new Exception($"Процесс скачивания прервался с кодом {process.ExitCode}");
    }

    // Метод для настройки параметров запуска процесса
    private ProcessStartInfo CreateStartInfo(string fileName, string arguments)
    {
        return new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true, 
            RedirectStandardInput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
    }
}