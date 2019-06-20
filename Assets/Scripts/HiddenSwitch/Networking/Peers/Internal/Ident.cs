using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenSwitch.Networking.Peers.Internal
{
    [Serializable]
    public class Ident : IComparable<Ident>, IEquatable<Ident>, IComparable
    {
        public long time;
        public Segment[] path;

        public Ident()
        {
        }

        public Ident(long time, Segment[] segments)
        {
            this.time = time;
            path = segments;
        }

        public Segment Get(int depth)
        {
            return path[depth];
        }

        public int depth => path.Length;

        public int CompareTo(Ident other)
        {
            var depth = Math.Max(path.Length, other.path.Length);
            for (var i = 0; i < depth; i++)
            {
                var hasMy = i < path.Length;
                var hasTheir = i < other.path.Length;
                if (!hasMy && hasTheir)
                {
                    return -1;
                }

                if (hasMy && !hasTheir)
                {
                    return 1;
                }

                var my = Get(i);
                var their = other.Get(i);
                if (my.digit < their.digit)
                {
                    return -1;
                }

                if (my.digit > their.digit)
                {
                    return 1;
                }

                var compare = string.CompareOrdinal(my.replica, their.replica);
                if (compare < 0)
                {
                    return -1;
                }

                if (compare > 0)
                {
                    return 1;
                }
            }

            if (time < other.time)
            {
                return -1;
            }

            if (time > other.time)
            {
                return 1;
            }

            return 0;
        }

        public bool Equals(Ident other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return time.Equals(other.time) && path.SequenceEqual(other.path);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Ident) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (time.GetHashCode() * 397) ^ (path != null ? path.GetHashCode() : 0);
            }
        }

        public static bool operator ==(Ident left, Ident right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Ident left, Ident right)
        {
            return !Equals(left, right);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is Ident other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Ident)}");
        }

        public static bool operator <(Ident left, Ident right)
        {
            return Comparer<Ident>.Default.Compare(left, right) < 0;
        }

        public static bool operator >(Ident left, Ident right)
        {
            return Comparer<Ident>.Default.Compare(left, right) > 0;
        }

        public static bool operator <=(Ident left, Ident right)
        {
            return Comparer<Ident>.Default.Compare(left, right) <= 0;
        }

        public static bool operator >=(Ident left, Ident right)
        {
            return Comparer<Ident>.Default.Compare(left, right) >= 0;
        }
    }
}