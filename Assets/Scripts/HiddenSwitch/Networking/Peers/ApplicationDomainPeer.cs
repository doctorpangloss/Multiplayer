using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace HiddenSwitch.Networking.Peers
{
    /// <summary>
    /// Represents a peer operating only in the local application domain. This is equivalent to a "local" only peer.
    /// </summary>
    public class ApplicationDomainPeer : IPeer, IDisposable
    {
        private static volatile int gameIdCounter;
        private static volatile int peerIdCounter;
        private static IReactiveCollection<IPeer> peers = new ReactiveCollection<IPeer>();
        private static List<IPeer> matchmakingPeers = new List<IPeer>();
        public int playersPerGame { get; set; } = 2;

        private static ReactiveCollection<GameContext> domainGames { get; } =
            new ReactiveCollection<GameContext>();

        private ReactiveProperty<PeerStatus> m_PeerStatus = new ReactiveProperty<PeerStatus>();

        public IReadOnlyReactiveCollection<GameContext> games
        {
            get => domainGames;
        }

        /// <summary>
        /// Awakes a peer in the local application domain.
        /// </summary>
        /// <returns></returns>
        public IObservable<PeerStatus> AwakeAsObservable()
        {
            peerId = (++peerIdCounter).ToString();
            m_PeerStatus.Value = new PeerStatus() {isConnected = true};
            peers.Add(this);
            return m_PeerStatus;
        }

        public IObservable<MatchmakingResult> Matchmake()
        {
            var queue = matchmakingPeers;
            var players = playersPerGame;
            var gameCollection = domainGames;
            var peerStatus = m_PeerStatus;
            
            if (queue.Count + 1 == players)
            {
                // Create the game
                var game = new ApplicationDomainGameContext();
                var gameId = ++gameIdCounter;
                // Populate useful base data
                game.data.Add(new Record()
                {
                    game = new World()
                    {
                        name = $"Game {gameId}",
                        gameId = gameId.ToString(),
                        status = GameStatus.Ready,
                        utcStartTime =
#if UNITY_WEBGL
                            // DateTime is glitched on WebGL mobile
                            (long)(Time.time*10e9)
#else
                            DateTime.UtcNow.Ticks
#endif
                    }
                });
                queue.Add(this);
                // Creates a player record for each peer that is now in this game
                foreach (var record in queue.Select((peer, i) => new Record()
                {
                    player = new PlayerRecord()
                    {
                        name = $"Peer {peer.peerId}",
                        peerId = peer.peerId,
                        playerId = i
                    }
                }))
                {
                    game.data.Add(record);
                }

                // Clears the "matchmaking" queue
                queue.Clear();
                // Call the game's create game hook
                game.OnMatchmakingCreatesGame();
                gameCollection.Add(game);
                // Updates the status of this peer
                peerStatus.Value = new PeerStatus()
                {
                    isConnected = true,
                    isMatchmaking = false,
                    isInGame = true
                };
                return Observable.Return(new MatchmakingResult()
                    {gameContext = game});
            }

            // Queue up
            queue.Add(this);
            peerStatus.Value = new PeerStatus()
            {
                isConnected = true,
                isMatchmaking = true,
                isInGame = false
            };

            // Wait for the game to be created that contains this peer
            return gameCollection
                .ToObservable()
                .Merge(gameCollection.ObserveAdd().Select(added => added.Value))
                .Where(game => game.data.Any(meta => meta.player?.peerId == peerId))
                .Do(game =>
                {
                    if (game.data.Any(meta => meta.game?.status == GameStatus.Ready))
                    {
                        peerStatus.Value = new PeerStatus()
                        {
                            isConnected = true,
                            isMatchmaking = false,
                            isInGame = true
                        };
                    }
                })
                .Select(game => new MatchmakingResult()
                {
                    gameContext = game
                })
                .Materialize()
                .SelectMany(notification =>
                {
                    // If the game is ready, make sure to complete this subscription
                    if (notification.Kind == NotificationKind.OnNext
                        && notification.Value.gameContext.data.Any(meta => meta.game?.status == GameStatus.Ready))
                    {
                        return new[] {notification, Notification.CreateOnCompleted<MatchmakingResult>()};
                    }

                    return new[] {notification};
                })
                .Dematerialize();
        }

        public IObservable<Unit> LeaveGracefully()
        {
            peers.Remove(this);
            m_PeerStatus.Value = new PeerStatus();
            foreach (var game in domainGames.Where(game => game.data.Any(meta => meta.player?.peerId == peerId))
            )
            {
                var myPlayerRecord = game.data.FirstOrDefault(meta => meta.player.peerId == peerId);
                game.data.Remove(myPlayerRecord);
            }

            return Observable.ReturnUnit();
        }

        public IReadOnlyReactiveProperty<PeerStatus> peerStatus => m_PeerStatus;

        public string peerId { get; private set; }

        ~ApplicationDomainPeer()
        {
            Dispose(false);
        }

        private void ReleaseUnmanagedResources()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                m_PeerStatus.Dispose();
                peers.Remove(this);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}