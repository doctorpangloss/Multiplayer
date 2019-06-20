using HiddenSwitch.Networking;

namespace HiddenSwitch.OneDimensionalChess
{
    public sealed class MoveEvent
    {
        public PieceView sender { get; set; }
        public Record record { get; set; }
        public int destination { get; set; }
    }
}