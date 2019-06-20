using System;
using System.Collections.Generic;
using System.Net.Sockets;
using HiddenSwitch.Networking.Peers.Internal;
using HiddenSwitch.Networking.Unity;
using Microsoft.AspNetCore.Hosting;
using UniRx;
using UniRx.Async;

namespace HiddenSwitch.Networking.Peers
{
    public class NetworkedServerPeer : IPeer, IDisposable
    {
        public string url { get; }

        private IWebHost m_WebHost;
        private ReactiveProperty<PeerStatus> m_PeerStatus = new ReactiveProperty<PeerStatus>();
        public IReadOnlyReactiveCollection<GameContext> games { get; }

        public NetworkedServerPeer(string url = "http://localhost:8001")
        {
            this.url = url;
        }
        
        /// <summary>
        /// Starts a web host capable of coordinating game traffic and matchmaking.
        /// </summary>
        /// <returns></returns>
        public IObservable<PeerStatus> AwakeAsObservable()
        {
            m_WebHost = SignalRServer<ReplicationHub>.Create<ReplicationHost>(url);
            peerId = url;
            return m_WebHost
                .StartAsync()
                .ToObservable()
                .Select(ignored => new PeerStatus()
                {
                    isConnected = true
                });
        }

        /// <summary>
        /// As a server-only peer, this peer cannot participate in matchmaking and will return a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <returns></returns>
        public IObservable<MatchmakingResult> Matchmake()
        {
            return Observable.Throw<MatchmakingResult>(new NotSupportedException());
        }

        public IObservable<Unit> LeaveGracefully()
        {
            return m_WebHost
                .StopAsync(TimeSpan.FromSeconds(1d))
                .ToObservable();
        }

        public IReadOnlyReactiveProperty<PeerStatus> peerStatus => m_PeerStatus;
        public string peerId { get; private set; }

        public void Dispose()
        {
            m_WebHost?.Dispose();
            m_PeerStatus?.Dispose();
        }
    }
}