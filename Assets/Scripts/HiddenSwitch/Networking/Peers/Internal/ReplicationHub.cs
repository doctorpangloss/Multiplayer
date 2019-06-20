using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using UnityEngine;

namespace HiddenSwitch.Networking.Peers.Internal
{
    internal class ReplicationHub : Hub
    {
        private ReplicationHost m_Host;

        public ReplicationHub(ReplicationHost host)
        {
            m_Host = host;
        }

        public async Task<string> OnMatchmake()
        {
            var peer = m_Host.peers[Context.ConnectionId];
            peer.internalPeerStatus.Value = new PeerStatus()
            {
                isConnected = true,
                isMatchmaking = true
            };
            var task = m_Host.Matchmake(Context.ConnectionId);
            var gameId = await task;

            await AddMeToGameGroup(gameId);

            SendMeReplica(gameId);
            // This doesn't wait for all clients to receive the initial data

            peer.internalPeerStatus.Value = new PeerStatus()
            {
                isConnected = true,
                isInGame = true
            };

            // This doesn't really permit you to cancel a task
            return await task;
        }

        protected Task SendMeReplica(string gameId)
        {
            return Clients.Caller.SendAsync(nameof(NetworkedClientPeer.ReceiveReplica), gameId,
                m_Host.games[gameId].replicatedData.atoms);
        }

        public void SendReplicationOp(string gameId, KSEQOperation<Record> op)
        {
            var game = m_Host.games[gameId];
            game.replicatedData.Apply(op);

            OthersInGame(gameId).SendAsync(nameof(NetworkedClientPeer.ReceiveReplicationOp), gameId, op).Start();
        }

        public override Task OnConnectedAsync()
        {
            // TODO: organize by auth to support reconnects
            var peer = new ReplicatedHostPeer(Context.ConnectionId);
            m_Host.peers[Context.ConnectionId] = peer;
            peer.internalPeerStatus.Value = new PeerStatus()
            {
                isConnected = true
            };
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var peer = m_Host.peers[Context.ConnectionId];
            peer.internalPeerStatus.Value = new PeerStatus();
            peer.Dispose();
            m_Host.peers.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        protected IClientProxy OthersInGame(string gameId)
        {
            return Clients.OthersInGroup($"game:{gameId}");
        }

        protected Task AddMeToGameGroup(string gameId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameId}");
        }
    }
}