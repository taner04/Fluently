namespace Fluently;

/// <summary>
///     How a dialog ended: closed with a <see cref="Value" />, or dismissed without one.
/// </summary>
/// <typeparam name="TResult">The type of the result the dialog returns.</typeparam>
public readonly record struct DialogResult<TResult>
{
    private DialogResult(TResult value)
    {
        IsCompleted = true;
        Value = value;
    }

    /// <summary>
    ///     A dialog that was dismissed without a result.
    /// </summary>
    public static DialogResult<TResult> Dismissed => default;

    /// <summary>
    ///     Whether the dialog was closed with a result. <see langword="false" /> when it was dismissed.
    /// </summary>
    public bool IsCompleted { get; }

    /// <summary>
    ///     The result the dialog was closed with, or <see langword="default" /> when it was dismissed.
    /// </summary>
    public TResult? Value { get; }

    /// <summary>
    ///     A dialog that was closed with <paramref name="value" />.
    /// </summary>
    /// <param name="value">The result the dialog was closed with.</param>
    /// <returns>A completed result with <see cref="Value" /> set to <paramref name="value" />.</returns>
    public static DialogResult<TResult> Completed(TResult value) => new(value);
}
