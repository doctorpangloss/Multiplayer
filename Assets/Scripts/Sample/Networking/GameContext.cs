using System;
using System.Linq;
using HiddenSwitch.OneDimensionalChess;
using UnityEngine;

namespace HiddenSwitch.Networking
{
    /// <summary>
    /// Your implementation of a game context.
    /// </summary>
    /// Use <see cref="OnGameAndPlayersReady"/> to initialize the data in your game.
    public partial class GameContext
    {
        partial void OnGameAndPlayersReady()
        {
            // Get the game record
            var gameRecord = data.First(r => r.game != null).game;
            var positions = new[]
            {
                new Tuple<int, PieceType>(0, PieceType.King),
                new Tuple<int, PieceType>(1, PieceType.Queen),
                new Tuple<int, PieceType>(2, PieceType.Rook),
                new Tuple<int, PieceType>(3, PieceType.Bishop),
                new Tuple<int, PieceType>(4, PieceType.Knight),
                new Tuple<int, PieceType>(5, PieceType.Pawn),
            };

            // Create both player's boards
            for (var playerId = 0; playerId < 2; playerId++)
            {
                var startPosition = playerId == 0 ? 0 : gameRecord.height - 1;
                var sign = playerId == 0 ? 1 : -1;
                foreach (var position in positions)
                {
                    var record = new Record()
                    {
                        piece = new Piece()
                        {
                            pieceType = position.Item2,
                            playerId = playerId,
                            position = startPosition + sign * position.Item1
                        }
                    };
                    // Observe SetId is called to make sure the records have valid, well-incremented IDs.
                    SetId(ref record);
                    data.Add(record);
                }
            }
            
            // In this example, players just replace the piece documents directly with the new positions, so it's up to
            // the clients to enforce the rules
        }
        
        public void OnMoveEvent(MoveEvent moveEvent)
        {
            // TODO: Do a real rules check. For now, don't allow pieces to occupy the same spot as other pieces
            if (data.Any(innerRecord => innerRecord.piece?.position == moveEvent.destination))
            {
                // Refresh using the original data, which wipes the client simulated value
                moveEvent.sender.pieceState = moveEvent.record;
            }
            else
            {
                var newRecord = new Record()
                {
                    id = moveEvent.record.id,
                    piece = new Piece()
                    {
                        playerId = moveEvent.record.piece.playerId,
                        pieceType = moveEvent.record.piece.pieceType,
                        position = moveEvent.destination
                    }
                };
                moveEvent.sender.pieceState = newRecord;
                ((IReactiveRecordCollection<Record>) data).Replace(newRecord);
            }
        }
    }
}