using System.Runtime.InteropServices;

namespace WabbajackDownloader.Features.WebView.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct NativePoint
{
    public int X;
    public int Y;

    public static implicit operator System.Drawing.Point(NativePoint np) => new(np.X, np.Y);
    public static implicit operator NativePoint(System.Drawing.Point p) => new(p.X, p.Y);

    public NativePoint() { }
    public NativePoint(int x, int y)
    {
        X = x;
        Y = y;
    }
}