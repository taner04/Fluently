namespace Fluently;

/// <summary>
///     How toasts behave, set with <c>services.AddFluently(options => options.Toasts…)</c>, which checks the values right
///     away. The toast service starts at <see cref="Position" /> and uses <see cref="Duration" /> and
///     <see cref="MaxCount" />
///     every time a toast is shown.
/// </summary>
public sealed class ToastOptions
{
    private static readonly TimeSpan MaxDuration = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    /// <summary>
    ///     Where toasts appear when the application starts. Defaults to <see cref="ToastPosition.TopRight" />; it can be
    ///     changed later with <see cref="IToastService.Position" />.
    /// </summary>
    public ToastPosition Position { get; set; } = ToastPosition.TopRight;

    /// <summary>
    ///     How long a toast is shown when <see cref="IToastService.Show(IToast, TimeSpan?)" /> is not given a duration.
    ///     Defaults to five seconds. Must be positive and at most 49 days, or <see cref="Timeout.InfiniteTimeSpan" /> to keep
    ///     toasts until they are dismissed.
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     How many toasts are shown at most; showing another removes the oldest. Defaults to five and must be at least one.
    /// </summary>
    public int MaxCount { get; set; } = 5;

    internal static bool IsValidDuration(TimeSpan duration) =>
        duration == Timeout.InfiniteTimeSpan || (duration > TimeSpan.Zero && duration <= MaxDuration);

    internal void Validate()
    {
        if (!Enum.IsDefined(Position))
        {
            throw new ArgumentOutOfRangeException("Toasts.Position", Position, "Not a ToastPosition.");
        }

        if (!IsValidDuration(Duration))
        {
            throw new ArgumentOutOfRangeException(
                "Toasts.Duration",
                Duration,
                "A toast needs a positive duration of at most 49 days, or Timeout.InfiniteTimeSpan.");
        }

        if (MaxCount < 1)
        {
            throw new ArgumentOutOfRangeException("Toasts.MaxCount", MaxCount, "At least one toast must be shown.");
        }
    }
}
