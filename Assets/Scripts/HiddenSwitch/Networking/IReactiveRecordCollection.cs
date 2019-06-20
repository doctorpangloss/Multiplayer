using UniRx;

namespace HiddenSwitch.Networking
{
    public interface IReactiveRecordCollection<T> : IReactiveCollection<T>
        where T : IId
    {
        bool Replace(T replacement);
    }
}