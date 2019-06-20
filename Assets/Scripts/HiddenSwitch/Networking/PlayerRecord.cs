using System;
using UnityEngine;

namespace HiddenSwitch.Networking
{
    [Serializable]
    public partial class PlayerRecord
    {
        public int playerId;
        public string peerId;
        public string name;
    }
}