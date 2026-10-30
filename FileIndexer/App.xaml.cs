using System.Windows;
using System.Windows.Threading;

namespace FileIndexer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show($"Необработанная ошибка: {args.Exception.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true; // Предотвращаем падение приложения
        };
    }
}