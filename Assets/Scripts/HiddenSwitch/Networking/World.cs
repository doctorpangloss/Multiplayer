using System;

namespace HiddenSwitch.Networking
{
    [Serializable]
    public partial class World
    {
        public string gameId;
        public string name;
        public long utcStartTime;
        public GameStatus status;
    }
}