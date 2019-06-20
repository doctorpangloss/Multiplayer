using System;

namespace HiddenSwitch.Networking.Peers.Internal
{
    [Serializable]
    public struct Segment : IEquatable<Segment>
    {
        public int digit;
        public string replica;

        public Segment(int digit, string replica)
        {
            this.digit = digit;
            this.replica = replica;
        }

        public bool Equals(Segment other)
        {
            return digit == other.digit && string.Equals(replica, other.replica);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Segment other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (digit * 397) ^ (replica != null ? replica.GetHashCode() : 0);
            }
        }

        public static bool operator ==(Segment left, Segment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Segment left, Segment right)
        {
            return !left.Equals(right);
        }
    }
}