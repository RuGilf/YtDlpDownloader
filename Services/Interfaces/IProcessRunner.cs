using System;
using System.Threading;
using System.Threading.Tasks;

namespace YtDlpDownloader.Services.Interfaces;

public interface IProcessRunner
{
    Task<string> RunAndGetOutputAsync(string fileName, string arguments);
    Task RunAndReadAsync(string fileName, string arguments, Action<string> onOutputReceived, CancellationToken cancellationToken);
}