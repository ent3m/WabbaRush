using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WabbajackDownloader.Common.Progress;

/// <summary>
/// A data container representing a progress report, with a minimum, maximum, and current value, as well as an optional title and description.
/// </summary>
/// <typeparam name="T">The type used to represent the progress value.</typeparam>
internal class ProgressDisplay<T> : IProgress<T>, INotifyPropertyChanged
{
    /// <summary>
    /// Get a value that represents the start of the progress range.
    /// </summary>
    public T Minimum
    {
        get;
        private set => SetField(ref field, value);
    }

    /// <summary>
    /// Get a value that represents the end of the progress range.
    /// </summary>
    public T Maximum
    {
        get;
        private set => SetField(ref field, value);
    }

    /// <summary>
    /// Get the current progress value.
    /// </summary>
    public T Value
    {
        get;
        private set => SetField(ref field, value);
    }

    /// <summary>
    /// Get or set a title for the operation being tracked.
    /// </summary>
    public string? Title
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>
    /// Get or set a description that provides additional context.
    /// </summary>
    public string? Description
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>
    /// Get or set the progress text format.
    /// </summary>
    public string? ProgressTextFormat
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>
    /// Get a value indicating whether the progress is indeterminate.
    /// </summary>
    public bool IsIndeterminate
    {
        get;
        private set => SetField(ref field, value);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ProgressDisplay{T}"/> with the specified range and optional metadata.
    /// </summary>
    /// <param name="minimum">The value that represents the start of the progress range. Also used as the initial <see cref="Value"/>.</param>
    /// <param name="maximum">The value that represents the end of the progress range.</param>
    /// <param name="title">An optional title for the operation being tracked.</param>
    /// <param name="description">An optional description providing additional context about the operation being tracked.</param>
    public ProgressDisplay(T minimum, T maximum, string? title = null, string? description = null, string? progressTextFormat = null)
    {
        Minimum = minimum;
        Maximum = maximum;
        Value = minimum;
        ProgressTextFormat = progressTextFormat;
        Title = title;
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ProgressDisplay{T}"/> with indeterminate progress and optional metadata.
    /// </summary>
    /// <param name="title">An optional title for the operation being tracked.</param>
    /// <param name="description">An optional description providing additional context about the operation being tracked.</param>
    public ProgressDisplay(string? title = null, string? description = null)
    {
        Minimum = Maximum = Value = default!; // Unused when progress is indeterminate
        IsIndeterminate = true;
        Title = title;
        Description = description;
    }

    public virtual void Report(T value) => Value = value;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<TField>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
