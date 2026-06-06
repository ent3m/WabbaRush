using Microsoft.Extensions.DependencyInjection;
using Waypoint;

namespace WabbajackDownloader.Common.Dialogs;

internal static class DialogsServiceExtensions
{
    public static IServiceCollection AddDialog(this IServiceCollection services) => services
        .AddTransient<ConfirmationDialog>()
        .AddTransient<ConfirmationDialogViewModel>()
        .AddTransient<ConfirmationWindow>()
        .AddTransient<ConfirmationWindowViewModel>()
        .AddTransient<Toast>()
        .AddTransient<ToastViewModel>();

    public static IViewRegistry RegisterDialogs(this IViewRegistry views) => views
        .Register<ConfirmationDialog, ConfirmationDialogViewModel>()
        .Register<ConfirmationWindow, ConfirmationWindowViewModel>()
        .Register<Toast, ToastViewModel>();
}