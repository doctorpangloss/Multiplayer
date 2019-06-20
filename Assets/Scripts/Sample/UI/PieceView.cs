using System;
using System.Linq;
using DG.Tweening;
using HiddenSwitch.Networking;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HiddenSwitch.OneDimensionalChess
{
    [RequireComponent(typeof(Graphic))]
    public class PieceView : UIBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        [SerializeField] private Image m_Image;
        [SerializeField] private float m_TweenDuration = 0.3f;
        [SerializeField] private PieceViewStyle[] m_Styles = new PieceViewStyle[0];
        [SerializeField] private BoardView m_BoardView;

        private ISubject<MoveEvent> m_MoveRequestedSubject = new Subject<MoveEvent>();
        private BehaviorSubject<Record> m_Record = new BehaviorSubject<Record>(new Record());
        public IObservable<MoveEvent> OnMoveRequestedAsObservable => m_MoveRequestedSubject;

        protected override void Start()
        {
            var rectTransform = ((RectTransform) transform);
            var topMiddle = new Vector2(0.5f, 1);
            rectTransform.anchorMin = topMiddle;
            rectTransform.anchorMax = topMiddle;
            Tweener tween = null;

            var dragging = false;
            m_Record
                .StartWith(new Record())
                .Pairwise()
                .Where(record => IsValid() && record.Current.piece != null)
                .Subscribe(pair =>
                {
                    // TODO: Handle a client simulated record somehow special
                    var value = pair.Current;
                    var previous = pair.Previous;
                    var thisStyle = m_Styles.First(style =>
                        style.playerId == value.piece.playerId && style.pieceType == value.piece.pieceType);
                    m_Image.sprite = thisStyle.sprite;
                    m_Image.SetNativeSize();
                    tween?.Kill();
                    tween = rectTransform.DOAnchorPos(m_BoardView.GetPieceCenterFromTop(value.piece.position),
                        m_TweenDuration);
                })
                .AddTo(this);


            // Handle dragging and dropping 
            this.OnBeginDragAsObservable()
                .Subscribe(pointer =>
                {
                    dragging = true;
                }).AddTo(this);

            Observable.EveryUpdate()
                .Where(ignored => dragging)
                .Subscribe(ignore =>
                {
                    var pointer = ContinuousStandaloneInputModule.instance.pointerEventData;
                    rectTransform.position = pointer.position;
                })
                .AddTo(this);

            this.OnEndDragAsObservable()
                .Subscribe(pointer =>
                {
                    dragging = false;
                    var destination = m_BoardView.GetClosestIndexToPointer(pointer);
                    if (destination == -1)
                    {
                        // Not a valid destination, refresh the record to return to the source location.
                        m_Record.OnNext(m_Record.Value);
                        return;
                    }

                    var currentPieceRecord = pieceState;
                    // Eagerly assume the move succeeded
                    m_Record.OnNext(new Record()
                    {
                        id = m_Record.Value.id,
                        piece = new Piece()
                        {
                            pieceType = m_Record.Value.piece.pieceType,
                            playerId = m_Record.Value.piece.playerId,
                            position = destination
                        }
                    });

                    // Now actually request the move. Ensures that in order, the real result will always come first.
                    m_MoveRequestedSubject.OnNext(new MoveEvent()
                    {
                        sender = this,
                        record = currentPieceRecord,
                        destination = destination
                    });
                }).AddTo(this);
        }

        public Record pieceState
        {
            get => m_Record.Value;
            set { m_Record.OnNext(value); }
        }

        private bool IsValid()
        {
            return m_Image != null && m_Styles.Length > 0 && m_BoardView != null;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
        }
    }
}