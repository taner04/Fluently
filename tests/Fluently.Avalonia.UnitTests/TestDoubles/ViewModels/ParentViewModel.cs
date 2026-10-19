namespace Fluently.Avalonia.UnitTests.TestDoubles.ViewModels;

[TransientPage]
public sealed class ParentViewModel : INavigableViewModel
{
    public ClockViewModel Child { get; } = new();
}
