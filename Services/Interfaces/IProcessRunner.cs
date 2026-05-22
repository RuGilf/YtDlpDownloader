using System;
using System.Threading.Tasks;

namespace YtDlpDownloader.Services.Interfaces;

// Интерфес для запуска консольных процессов и перенаправления их вывода
public interface IProcessRunner
{
    Task<string> RunAndGetOutputAsync(string fileName, string arguments);
    Task RunAndReadAsync(string fileName, string arguments, Action<string> onOutputReceived);
}