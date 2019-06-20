using System;

namespace HiddenSwitch.OneDimensionalChess
{
    [Serializable]
    public class Piece
    {
        public PieceType pieceType;
        public int position;
        public int playerId;
    }
}