using UniRx;

namespace HiddenSwitch.Networking
{
    public interface IReadOnlyGame
    {
        IReadOnlyReactiveCollection<Record> data { get; }
    }
}