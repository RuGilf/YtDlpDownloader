using Avalonia.Interactivity; // Нужно для RoutedEventArgs
using SukiUI;                 // Нужно для SukiTheme
using SukiUI.Controls;

namespace YtDlpDownloader.Views;

public partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Обработчик нажатия на кнопку "Сменить тему"
    private void ToggleTheme_Click(object? sender, RoutedEventArgs e)
    {
        // Переключаем системную тему SukiUI на лету
        SukiTheme.GetInstance().SwitchBaseTheme();
    }
}