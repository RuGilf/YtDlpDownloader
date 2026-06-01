using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace YtDlpDownloader.Services.Interfaces;

public interface IImageLoader
{
    Task<Bitmap?> LoadFromUrlAsync(string url, CancellationToken cancellationToken = default);
}
