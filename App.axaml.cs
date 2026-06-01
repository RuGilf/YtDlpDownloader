using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using YtDlpDownloader.ViewModels;
using YtDlpDownloader.Views;
using System.Diagnostics;
using YtDlpDownloader.Services.Implementations;

namespace YtDlpDownloader;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var processRunner = new ProcessRunner();
            var ytDlpService = new YtDlpService(processRunner);
            var dependencyManager = new DependencyManager(processRunner);
            var fileDialogService = new FileDialogService();
            var downloadQueueManager = new DownloadQueueManager(ytDlpService);
            var imageLoader = new ImageLoader();
            var downloadTaskFactory = new DownloadTaskFactory();

            var mainWindowViewModel = new MainWindowViewModel(
                ytDlpService,
                dependencyManager,
                fileDialogService,
                downloadQueueManager,
                imageLoader,
                downloadTaskFactory);

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
