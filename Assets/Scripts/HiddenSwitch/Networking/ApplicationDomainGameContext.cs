using UniRx;

namespace HiddenSwitch.Networking
{
    /// <summary>
    /// A game context shared by all the peers in the same application domain. Equivalent to a "local" game.
    /// </summary>
    internal sealed class ApplicationDomainGameContext : GameContext
    {
        private IReactiveRecordCollection<Record> m_Data = new ReactiveRecordCollection();

        public override IReactiveCollection<Record> data => m_Data;
    }
}