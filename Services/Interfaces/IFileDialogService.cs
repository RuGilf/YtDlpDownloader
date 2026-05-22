using System.Threading.Tasks;

namespace YtDlpDownloader.Services.Implementations;

public interface IFileDialogService
{
    Task<string?> SelectFolderAsync();
}