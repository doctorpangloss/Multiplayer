using System;

namespace HiddenSwitch.Networking
{
    [Serializable]
    public partial struct Record : IId
    {
        public int _id;
        public World game;
        public PlayerRecord player;

        public int id
        {
            get { return _id; }
            set { _id = value; }
        }
    }
}