using System;
using UniRx;

namespace HiddenSwitch.Networking.Peers.Internal
{
    internal sealed class ReplicatedHostPeer : IPeer, IDisposable
    {
        public ReplicatedHostPeer(string contextConnectionId)
        {
            peerId = contextConnectionId;
        }

        public IReadOnlyReactiveCollection<GameContext> games { get; }

        public IObservable<PeerStatus> AwakeAsObservable()
        {
            throw new NotImplementedException();
        }

        public IObservable<MatchmakingResult> Matchmake()
        {
            throw new NotImplementedException();
        }

        public IObservable<Unit> LeaveGracefully()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyReactiveProperty<PeerStatus> peerStatus => internalPeerStatus;

        internal ReactiveProperty<PeerStatus> internalPeerStatus { get; set; } = new ReactiveProperty<PeerStatus>();
        public string peerId { get; }

        public void Dispose()
        {
        }
    }
}