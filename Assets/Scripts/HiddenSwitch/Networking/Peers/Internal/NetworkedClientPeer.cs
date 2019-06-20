using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Client;
using UniRx;

namespace HiddenSwitch.Networking.Peers.Internal
{
    internal sealed class NetworkedClientPeer : IPeer, IDisposable
    {
        private class ReactiveReplicatedGameCollection : ReactiveCollection<ReplicatedGameContext>,
            IReadOnlyReactiveCollection<GameContext>
        {
            IEnumerator<GameContext> IEnumerable<GameContext>.GetEnumerator()
            {
                using (var enumerator = base.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        yield return enumerator.Current;
                    }
                }
            }

            public GameContext this[int index] => base[index];

            public IObservable<CollectionAddEvent<GameContext>> ObserveAdd()
            {
                return base.ObserveAdd()
                    .Select(added => new CollectionAddEvent<GameContext>(added.Index, added.Value));
            }

            public IObservable<CollectionMoveEvent<GameContext>> ObserveMove()
            {
                return base.ObserveMove()
                    .Select(moved => new CollectionMoveEvent<GameContext>(moved.OldIndex, moved.NewIndex, moved.Value));
            }

            public IObservable<CollectionRemoveEvent<GameContext>> ObserveRemove()
            {
                return base.ObserveRemove()
                    .Select(removed => new CollectionRemoveEvent<GameContext>(removed.Index, removed.Value));
            }

            public IObservable<CollectionReplaceEvent<GameContext>> ObserveReplace()
            {
                return base.ObserveReplace()
                    .Select(replaced =>
                        new CollectionReplaceEvent<GameContext>(replaced.Index, replaced.OldValue, replaced.NewValue));
            }
        }

        private readonly string m_Url;
        private HubConnection m_HubConnection;
        private CompositeDisposable m_CompositeDisposable = new CompositeDisposable();
        private ReactiveReplicatedGameCollection m_Games = new ReactiveReplicatedGameCollection();
        private ReactiveProperty<PeerStatus> m_PeerStatus = new ReactiveProperty<PeerStatus>();

        public NetworkedClientPeer(string url)
        {
            m_Url = url;
        }

        public IReadOnlyReactiveCollection<GameContext> games => m_Games;

        public IObservable<PeerStatus> AwakeAsObservable()
        {
            m_HubConnection = new HubConnectionBuilder()
                .WithUrl(m_Url + HubConnectionExtensions.Path())
                .Build();

            m_HubConnection.On(nameof(ReceiveReplicationOp),
                new Action<string, KSEQOperation<Record>>(ReceiveReplicationOp));

            m_HubConnection.On(nameof(ReceiveReplica),
                new Action<string, List<Atom<Record>>>(ReceiveReplica));


            return m_HubConnection
                .StartAsync()
                .ToObservable()
                .Take(1)
                .Do(ignored => { peerId = m_HubConnection.GetConnectionId(); })
                .Select(ignored => new PeerStatus()
                    {isConnected = m_HubConnection.State == HubConnectionState.Connected});
        }

        public IObservable<MatchmakingResult> Matchmake()
        {
            return m_HubConnection.InvokeAsync<string>(nameof(ReplicationHub.OnMatchmake))
                .ToObservable()
                .Take(1)
                .DoOnSubscribe(() =>
                {
                    m_PeerStatus.Value = new PeerStatus()
                    {
                        isConnected = true,
                        isMatchmaking = true,
                        isInGame = false
                    };
                })
                .SelectMany(gameId =>
                {
                    return games.ToObservable()
                        .Merge(m_Games.ObserveAdd().Select(added => added.Value))
                        .Where(game => game.gameId == gameId);
                })
                .Do(game =>
                {
                    m_PeerStatus.Value = new PeerStatus()
                    {
                        isConnected = true,
                        isMatchmaking = false,
                        isInGame = true
                    };
                })
                .Select(game => new MatchmakingResult() {gameContext = game})
                .Take(1);
        }

        public IObservable<Unit> LeaveGracefully()
        {
            return Observable.ReturnUnit();
        }

        public IReadOnlyReactiveProperty<PeerStatus> peerStatus => m_PeerStatus;
        public string peerId { get; private set; }

        public void Dispose()
        {
            m_CompositeDisposable.Dispose();
            LeaveGracefully().Take(1).Subscribe();
            m_HubConnection?.DisposeAsync().Start();
        }

        public void ReceiveReplicationOp(string gameId, KSEQOperation<Record> op)
        {
            ((ReplicatedGameContext) games.First(g => g.gameId == gameId)).replicatedData.Apply(op);
        }

        public void ReceiveReplica(string gameId, List<Atom<Record>> records)
        {
            var replicatedGame = new ReplicatedGameContext(gameId, m_HubConnection.GetConnectionId(), records);
            m_Games.Add(replicatedGame);

            // Send out any writes we make to this replicated game from the client.
            Observable.Merge(replicatedGame.replicatedData.ObserveThisReplicaAdded().AsUnitObservable(),
                    replicatedGame.replicatedData.ObserveThisReplicaReplaced().AsUnitObservable(),
                    replicatedGame.replicatedData.ObserveThisReplicaRemoved().AsUnitObservable())
                .SelectMany(event_ => OnReplicationEvent(gameId, replicatedGame))
                .Subscribe()
                .AddTo(m_CompositeDisposable);
        }

        private IObservable<Unit> OnReplicationEvent(string gameId, ReplicatedGameContext replicatedGameContext)
        {
            return m_HubConnection.InvokeAsync(nameof(ReplicationHub.SendReplicationOp), gameId,
                replicatedGameContext.replicatedData.lastOp.Value).ToObservable().Take(1);
        }
    }
}