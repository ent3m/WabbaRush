using Avalonia.Controls;
using System;
using System.Numerics;

namespace WabbajackDownloader.Core;

/// <summary>
/// Progress that is displayed by a progress bar
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