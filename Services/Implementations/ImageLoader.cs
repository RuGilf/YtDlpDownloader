using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using YtDlpDownloader.Services.Interfaces;

namespace YtDlpDownloader.Services.Implementations;

public class ImageLoader : IImageLoader
{
    private const int MaxImageBytes = 10 * 1024 * 1024;

    private static readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public async Task<Bitmap?> LoadFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }
        
        try
        {
            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength > MaxImageBytes ||
                response.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true)
            {
                return null;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var stream = new MemoryStream();
            var buffer = new byte[81920];
            int read;

            while ((read = await responseStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (stream.Length + read > MaxImageBytes)
                {
                    return null;
                }

                stream.Write(buffer, 0, read);
            }

            stream.Position = 0;

            return new Bitmap(stream);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }
}
