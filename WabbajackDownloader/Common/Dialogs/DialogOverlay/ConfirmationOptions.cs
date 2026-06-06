namespace WabbajackDownloader.Common.Dialogs;

internal sealed record ConfirmationOptions(
    string Title,
    string Message,
    string ConfirmMessage = "Yes",
    string CancelMessage = "No",
    Avalonia.Media.Geometry? Icon = null);