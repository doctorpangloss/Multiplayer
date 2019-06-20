using System.Collections;
using UniRx;

namespace HiddenSwitch.Networking
{
    public interface IGameContext : IReadOnlyGame
    {
        string gameId { get; }
        IReactiveCollection<Record> data { get; }

        /// <summary>
        /// Called exactly once on a single peer when the game has all its players connected. This means there should
        /// be a record with game data and player data corresponding to each player in its data field after this method
        /// is called.
        /// </summary>
        void OnMatchmakingCreatesGame();
    }
}