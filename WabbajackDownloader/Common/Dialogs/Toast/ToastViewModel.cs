using Waypoint;

namespace WabbajackDownloader.Common.Dialogs;

internal sealed partial class ToastViewModel : INavigable
{
    public string? Message { get; private set; }
    public ToastType Type { get; private set; }
    public int FontSize { get; private set; } = 14;

    public bool IsError => Type == ToastType.Error;
    public bool IsInformation => Type == ToastType.Information;
    public bool IsNeutral => Type == ToastType.Neutral;
    public bool IsSuccess => Type == ToastType.Success;
    public bool IsWarning => Type == ToastType.Warning;

    Task INavigable.OnNavigatingToAsync(object? parameter, CancellationToken cancellationToken)
    {
        if (parameter is string message)
        {
            Message = message;
        }
        else if (parameter is ToastOptions options)
        {
            Message = options.Message;
            Type = options.Type;
            FontSize = options.FontSize;
        }
        return Task.CompletedTask;
    }
}
