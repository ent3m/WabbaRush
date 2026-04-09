using System.Drawing;
using System.Runtime.InteropServices;

namespace WabbajackDownloader.Features.WebView.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct NativePoint
{
    public int X;
    public int Y;

    public static implicit operator Point(NativePoint np) => new(np.X, np.Y);
    public static implicit operator NativePoint(Point p) => new() { X = p.X, Y = p.Y };
}