namespace WabbajackDownloader.Features.WebView;

internal interface IAttachableProperty<T> where T : class
{
    void Attach(T target);
    void Detach();
}