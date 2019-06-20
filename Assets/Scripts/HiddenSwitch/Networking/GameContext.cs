using UniRx;

namespace HiddenSwitch.Networking
{
    /// <summary>
    /// Base implementation of a game context.
    /// </summary>
    public abstract partial class GameContext : IGameContext
    {
        public string gameId { get; set; }
        public abstract IReactiveCollection<Record> data { get; }
        IReadOnlyReactiveCollection<Record> IReadOnlyGame.data => data;

        public void OnMatchmakingCreatesGame()
        {
            OnGameAndPlayersReady();
        }

        /// <summary>
        /// The user's implementation to add custom data into the game from an authoritative (read/write) peer.
        /// </summary>
        partial void OnGameAndPlayersReady();

        public virtual void SetId(ref Record record)
        {
        }
    }
}