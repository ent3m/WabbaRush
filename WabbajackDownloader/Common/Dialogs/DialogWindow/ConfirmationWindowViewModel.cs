using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Waypoint;

namespace WabbajackDownloader.Common.Dialogs;

internal sealed partial class ConfirmationWindowViewModel : ObservableObject, INavigable
{
    [DialogResult(typeof(ConfirmationWindow))]
    public event Action<bool>? CloseRequested;

    public string Title { get; private set; } = "Confirm Action";
    public string Message { get; private set; } = "Are you sure you want to proceed?";
    public string ConfirmMessage { get; private set; } = "Yes";
    public string CancelMessage { get; private set; } = "No";
    public Geometry? Icon { get; private set; } = null;

    [RelayCommand]
    private void Confirm()
    {
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }

    Task INavigable.OnNavigatingToAsync(object? parameter, CancellationToken cancellationToken)
    {
        if (parameter is string message)
        {
            Message = message;
        }
        else if (parameter is ConfirmationOptions options)
        {
            Title = options.Title;
            Message = options.Message;
            ConfirmMessage = options.ConfirmMessage;
            CancelMessage = options.CancelMessage;
            Icon = options.Icon;
        }
        return Task.CompletedTask;
    }
}
