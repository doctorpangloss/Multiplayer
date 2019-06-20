using System;

namespace HiddenSwitch.Networking
{
    [Serializable]
    public sealed class MatchmakingResult
    {
        public IGameContext gameContext { get; set; }
    }
}