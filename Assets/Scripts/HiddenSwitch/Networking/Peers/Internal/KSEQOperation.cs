using System;

namespace HiddenSwitch.Networking.Peers.Internal
{
    [Serializable]
    public struct KSEQOperation<T>
    {
        public KSEQOperationTypes op;
        public string replicaId;
        public long realTime;
        public Ident id;
        public T value;
    }
}