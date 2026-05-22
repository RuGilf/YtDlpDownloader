using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace YtDlpDownloader.Services;

public static class ImageLoader
{
    private static readonly HttpClient httpClient = new();

    public static async Task<Bitmap?> LoadFromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        
        try
        {
            var bytes = await httpClient.GetByteArrayAsync(url);
            using var stream = new MemoryStream(bytes);

            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }
}