using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace YtDlpDownloader.Services.Implementations;

public class FileDialogService : IFileDialogService
{
    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    public async Task<string?> SelectFolderAsync()
    {
        var window = GetMainWindow();
        if (window == null)
            return null;

        var options = new FolderPickerOpenOptions
        {
            Title = "Выберите папку для сохранения видео",
            AllowMultiple = false
        };

        var result = await window.StorageProvider.OpenFolderPickerAsync(options);

        if (result.Count > 0)
            return result[0].Path.LocalPath;
        
        return null;
    }
}