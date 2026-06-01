using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YtDlpDownloader.Services.Interfaces;

public interface IProcessRunner
{
    Task<string> RunAndGetOutputAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default);
    Task RunAndReadAsync(string fileName, IReadOnlyList<string> arguments, Action<string> onOutputReceived, CancellationToken cancellationToken);
}
