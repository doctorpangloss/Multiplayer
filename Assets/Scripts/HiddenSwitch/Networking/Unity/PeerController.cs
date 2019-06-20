using System;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HiddenSwitch.Networking
{
    public class PeerController : UIBehaviour
    {
        [SerializeField] private Camera m_Camera;
        [SerializeField] private Canvas m_Canvas;
        private IPeer m_Peer;
        private ReactiveReadOnly<int> m_PlayerId;
        private ReactiveReadOnly<GameContext> m_Game;

        public ReactiveReadOnly<GameContext> game => m_Game;

        public IObservable<GameContext> OnGameReady()
        {
            return game;
        }

        public ReactiveReadOnly<int> playerId => m_PlayerId;

        public new Camera camera => m_Camera;
        public Canvas canvas => m_Canvas;

        public IPeer peer
        {
            get { return m_Peer; }
            set
            {
                m_Peer = value;
                m_PlayerId = new ReactiveReadOnly<int>(peer.games.ToObservableAndAdded()
                    .SelectMany(g =>
                        g.data.Where(r => r.player?.peerId == peer.peerId)
                            .Select(r => r.player.playerId)));
                m_Game = new ReactiveReadOnly<GameContext>(m_Peer?.games.ToObservableAndAdded());
            }
        }
    }
}