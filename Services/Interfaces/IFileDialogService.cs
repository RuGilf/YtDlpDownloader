using System.Threading.Tasks;

namespace YtDlpDownloader.Services.Interfaces;

public interface IFileDialogService
{
    Task<string?> SelectFolderAsync();
}
