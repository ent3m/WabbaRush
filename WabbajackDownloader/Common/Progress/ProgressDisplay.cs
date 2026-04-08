using Avalonia.Controls;
using System.Numerics;

namespace WabbajackDownloader.Common.Progress;

/// <summary>
/// IProgress that is coupled with a ProgressBar
/// </summary>
internal class ProgressDisplay<T> where T : INumber<T>
{
    public IProgress<T> Progress { get; }
    public ProgressBar ProgressBar { get; }

    public ProgressDisplay()
    {
        ProgressBar = new ProgressBar
        {
            ShowProgressText = true
        };
        Progress = new Progress<T>(i => ProgressBar.Value = Convert.ToDouble(i));
    }
}