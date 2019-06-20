using System;
using UnityEngine;

namespace HiddenSwitch.OneDimensionalChess
{
    [Serializable]
    public class PieceViewStyle
    {
        [SerializeField] private PieceType m_PieceType;
        [SerializeField] private int m_PlayerId;
        [SerializeField] private Sprite m_Sprite;

        public PieceType pieceType => m_PieceType;

        public int playerId => m_PlayerId;

        public Sprite sprite => m_Sprite;
    }
}