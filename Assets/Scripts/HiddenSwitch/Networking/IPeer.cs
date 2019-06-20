using System;
using UniRx;

namespace HiddenSwitch.Networking
{
    public interface IPeer
    {
        /// <summary>
        /// All the games that are visible to this peer.
        /// </summary>
        /// As a client, peers can typically see only the games they are participating in. As a server, peers can see
        /// all the games they are hosting. As a clustered server, peers can see all the games in the cluster.
        IReadOnlyReactiveCollection<GameContext> games { get; }

        /// <summary>
        /// Discovers and connects to other peers, maintaining the connection as reliably as possible.
        /// </summary>
        /// For now, finds a static field that represents the cluster running on the same application domain.
        /// <returns>The result of connecting</returns>
        IObservable<PeerStatus> AwakeAsObservable();

        /// <summary>
        /// Enters a matchmaking queue. Returns various status updates.
        /// </summary>
        /// Games which use lobbies will yield multiple <see cref="MatchmakingResult"/> items. When in a lobby but not
        /// all players have connected, the result will have a status of <see cref="GameStatus.AwaitingConnections"/>.
        ///
        /// Games which do not use lobbies will only yield a result with a <see cref="GameStatus.Ready"/> game meta
        /// record.
        /// <returns></returns>
        IObservable<MatchmakingResult> Matchmake();

        /// <summary>
        /// Leaves the peering gracefully.
        /// </summary>
        /// This disconnects from the peering group. As a client, this disconnects from the server. As a server in any
        /// configuration, it will also close all the matches this server is running. It will yield if a leave was
        /// succeeded.
        IObservable<Unit> LeaveGracefully();

        /// <summary>
        /// Gets the current status of the peer.
        /// </summary>
        IReadOnlyReactiveProperty<PeerStatus> peerStatus { get; }

        /// <summary>
        /// The peer ID. Typically corresponds to a user ID or a server ID.
        /// </summary>
        string peerId { get; }
    }
}