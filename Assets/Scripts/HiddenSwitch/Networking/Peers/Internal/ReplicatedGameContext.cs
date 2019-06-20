using System.Collections.Generic;
using UniRx;

namespace HiddenSwitch.Networking.Peers.Internal
{
    internal sealed class ReplicatedGameContext : GameContext
    {
        public override IReactiveCollection<Record> data => replicatedData;
        public ReplicatedReactiveRecordCollection<Record> replicatedData { get; }

        public ReplicatedGameContext(string gameId, string replicaId, IList<Atom<Record>> replica = null)
        {
            this.gameId = gameId;
            replicatedData = new ReplicatedReactiveRecordCollection<Record>(replicaId, replica);
        }

        public override void SetId(ref Record record)
        {
            replicatedData.SetId(ref record);
        }
    }
}