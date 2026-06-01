using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YtDlpDownloader.Services.Interfaces;

namespace YtDlpDownloader.Services.Implementations;

public class ProcessRunner : IProcessRunner
{
    public async Task<string> RunAndGetOutputAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        var startInfo = CreateStartInfo(fileName, arguments);

        using var process = new Process { StartInfo = startInfo };
        
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var outputLock = new object();
        var errorLock = new object();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                lock (outputLock)
                {
                    outputBuilder.AppendLine(args.Data);
                }
            }
        };
        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                lock (errorLock)
                {
                    errorBuilder.AppendLine(args.Data);
                }
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Не удалось запустить процесс: {fileName}");
        }

        process.StandardInput.Close();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch { }
        });

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new Exception($"Процесс завершился с ошибкой (код {process.ExitCode}).\nДетали: {errorBuilder}");
        }

        lock (outputLock)
        {
            return outputBuilder.ToString();
        }
    }

    public async Task RunAndReadAsync(string fileName, IReadOnlyList<string> arguments, Action<string> onOutputReceived, CancellationToken cancellationToken)
    {
        var startInfo = CreateStartInfo(fileName, arguments);

        using var process = new Process { StartInfo = startInfo };
        var errorBuilder = new StringBuilder();
        var errorLock = new object();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                onOutputReceived(args.Data);
            }
        };
        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                lock (errorLock)
                {
                    errorBuilder.AppendLine(args.Data);
                }

                onOutputReceived(args.Data);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Не удалось запустить процесс: {fileName}");
        }

        process.StandardInput.Close();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true); 
                }
            }
            catch { }
        });

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw new Exception("Скачивание было отменено пользователем.");
        }

        if (process.ExitCode != 0 && !cancellationToken.IsCancellationRequested)
        {
            string details;
            lock (errorLock)
            {
                details = errorBuilder.ToString();
            }

            throw new Exception($"Процесс скачивания прервался с кодом {process.ExitCode}.\nДетали: {details}");
        }
    }

    private ProcessStartInfo CreateStartInfo(string fileName, IReadOnlyList<string> arguments)
    {
        if (!Path.IsPathFullyQualified(fileName))
        {
            throw new InvalidOperationException($"Для запуска процесса требуется абсолютный путь: {fileName}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
