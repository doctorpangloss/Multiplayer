using System.Linq;
using HiddenSwitch.Networking;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HiddenSwitch.OneDimensionalChess
{
    public class BoardView : UIBehaviour
    {
        [SerializeField] private Record m_Record;
        [SerializeField] private float m_TileSize = 37.5f;
        [SerializeField] private RawImage m_Image;
        [SerializeField] private int m_TilesPerUV = 8;

        public Record record
        {
            get => m_Record;
            set
            {
                m_Record = value;
                SetDirty();
            }
        }


        private int m_PlayerId;

        public int playerId
        {
            get => m_PlayerId;
            set
            {
                m_PlayerId = value;
                SetDirty();
            }
        }

        private int gameHeight => (m_Record.game?.height ?? 14);

        private void SetDirty()
        {
            var gameHeight = this.gameHeight;
            var playerId = this.playerId;

            var rectTransform = (RectTransform) transform;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_TileSize * gameHeight);
            m_Image.uvRect = new Rect(0, playerId == 1 ? 0 : 1f / m_TilesPerUV, 1f / m_TilesPerUV,
                (float) gameHeight / m_TilesPerUV);
        }

        public Vector2 GetPieceCenterFromTop(int logicalPosition)
        {
            if (playerId == 0)
            {
                logicalPosition = gameHeight - logicalPosition - 1;
            }

            var rect = ((RectTransform) transform).rect;
            // Center points on a grid
            var tileHeight = rect.height / gameHeight;
            return Vector2.down * (tileHeight / 2 + logicalPosition * tileHeight);
        }

        public int GetClosestIndexToPointer(PointerEventData pointer)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform) transform, pointer.position,
                pointer.enterEventCamera, out var localPoint))
            {
                return -1;
            }

            return Enumerable.Range(0, gameHeight)
                .OrderBy(i =>
                {
                    var pieceCenterFromTop = GetPieceCenterFromTop(i);
                    return Vector2.SqrMagnitude(pieceCenterFromTop - localPoint);
                })
                .First();
        }
    }
}