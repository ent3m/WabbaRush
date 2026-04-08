using Avalonia.Controls;

namespace WabbajackDownloader.Features.Dashboard;

internal partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnClosing(e);
    }
}
