using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace HiddenSwitch.Networking.Peers.Internal
{
    internal sealed class ReplicationHost : IDisposable
    {
        private const string HostReplicaId = "_";

        public struct QueueEntry
        {
            public string peerId { get; set; }
            public TaskCompletionSource<string> matchmakingEntry { get; set; }
        }

        public int playersPerGame { get; set; } = 2;

        public List<QueueEntry> queue { get; }

        public IDictionary<string, ReplicatedHostPeer> peers { get; }
        private IHubContext<ReplicationHub> hub { get; }
        public IDictionary<string, ReplicatedGameContext> games { get; }

        public ReplicationHost(IHubContext<ReplicationHub> hub)
        {
            this.hub = hub;
            peers = new ConcurrentDictionary<string, ReplicatedHostPeer>();
            games = new ConcurrentDictionary<string, ReplicatedGameContext>();
            queue = new List<QueueEntry>();
        }

        public void Dispose()
        {
            foreach (var kv in peers)
            {
                kv.Value.Dispose();
            }

            peers.Clear();
        }

        public Task<string> Matchmake(string peerId, CancellationToken cancellationToken = default)
        {
            var thisTask = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (queue)
            {
                if (queue.Count + 1 == playersPerGame)
                {
                    // Create the game
                    var gameId = Guid.NewGuid().ToString();
                    var game = new ReplicatedGameContext(gameId, HostReplicaId);
                    // Populate useful base data
                    var rec = new Record()
                    {
                        game = new World()
                        {
                            name = $"Game {gameId}",
                            gameId = gameId,
                            status = GameStatus.Ready,
                            utcStartTime = DateTime.UtcNow.Ticks
                        }
                    };
                    game.replicatedData.SetId(ref rec);
                    game.replicatedData.Add(rec);
                    queue.Add(new QueueEntry() {matchmakingEntry = thisTask, peerId = peerId});
                    // Creates a player record for each peer that is now in this game
                    foreach (var record in queue.Select((peer, i) =>
                    {
                        var rec2 = new Record()
                        {
                            player = new PlayerRecord()
                            {
                                name = $"Peer {peer.peerId}",
                                peerId = peer.peerId,
                                playerId = i
                            }
                        };
                        game.replicatedData.SetId(ref rec2);
                        return rec2;
                    }))
                    {
                        game.replicatedData.Add(record);
                    }

                    // Call the game's create game hook
                    game.OnMatchmakingCreatesGame();
                    games.Add(gameId, game);

                    // Finish matchmaking for all players
                    foreach (var record in queue)
                    {
                        record.matchmakingEntry.SetResult(gameId);
                    }

                    // Clears the "matchmaking" queue
                    queue.Clear();
                    // This is completed
                    return thisTask.Task;
                }

                // Queue up
                queue.Add(new QueueEntry() {peerId = peerId, matchmakingEntry = thisTask});
            }

            return thisTask.Task;
        }
    }
}