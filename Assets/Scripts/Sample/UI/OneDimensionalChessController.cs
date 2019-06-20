using System.Collections.Generic;
using System.Linq;
using HiddenSwitch.Networking;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HiddenSwitch.OneDimensionalChess
{
    public class OneDimensionalChessController : UIBehaviour
    {
        [SerializeField] private BoardView m_BoardView;
        [SerializeField] private PieceView m_PieceViewPrefab;
        [SerializeField] private PeerController m_PeerController;

        protected override void Start()
        {
            var gameLifetime = new CompositeDisposable();
            m_PeerController.OnGameReady()
                .Subscribe(game =>
                {
                    gameLifetime?.Dispose();
                    gameLifetime = new CompositeDisposable();
                    m_BoardView.playerId = m_PeerController.playerId;
                    var pieceViews = new Dictionary<int, PieceView>();
                    // Keep the board updated
                    gameContext.data.ToObservableAddedAndReplaced()
                        .Where(record => record.game != null)
                        .Subscribe(record => { m_BoardView.record = record; })
                        .AddTo(gameLifetime);

                    // Create pieces for each instance
                    gameContext.data.ToObservableAndAdded()
                        .Where(record => record.piece != null)
                        .Subscribe(record =>
                        {
                            var piece = Instantiate(m_PieceViewPrefab, m_BoardView.transform);
                            // Actually handle dragging and dropping pieces
                            piece.OnMoveRequestedAsObservable
                                .Subscribe(gameContext.OnMoveEvent)
                                .AddTo(piece);
                            var id = record.id;
                            pieceViews[id] = piece;
                            piece.pieceState = record;
                        }).AddTo(gameLifetime);

                    // Keep the pieces updated
                    gameContext.data.ObserveReplace()
                        .Where(replaced => replaced.NewValue.piece != null)
                        .Subscribe(replaced => { pieceViews[replaced.NewValue.id].pieceState = replaced.NewValue; })
                        .AddTo(gameLifetime);

                    // Remove pieces we don't need anymore
                    gameContext.data.ObserveRemove()
                        .Where(removed => removed.Value.piece != null)
                        .Subscribe(removed =>
                        {
                            Destroy(pieceViews[removed.Value.id].gameObject);
                            pieceViews.Remove(removed.Value.id);
                        })
                        .AddTo(gameLifetime);
                })
                .AddTo(this);
        }

        public GameContext gameContext => m_PeerController.game.Value;


    }
}