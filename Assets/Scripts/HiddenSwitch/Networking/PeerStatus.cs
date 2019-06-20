using System;

namespace HiddenSwitch.Networking
{
    [Serializable]
    public struct PeerStatus
    {
        public bool isMatchmaking;
        public bool isInGame;
        public bool isConnected;
    }
}