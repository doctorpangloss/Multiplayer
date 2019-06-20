using System;
using System.Collections;
using System.Collections.Generic;

namespace HiddenSwitch.Networking.Peers.Internal
{
    [Serializable]
    public struct Atom<T> : IEquatable<Atom<T>>, IComparable<Atom<T>>, IComparable
    {
        public Ident id { get; set; }
        public T value { get; set; }

        public Atom(Ident id, T value)
        {
            this.id = id;
            this.value = value;
        }

        public bool Equals(Atom<T> other)
        {
            return Equals(id, other.id) && EqualityComparer<T>.Default.Equals(value, other.value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Atom<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((id != null ? id.GetHashCode() : 0) * 397) ^ EqualityComparer<T>.Default.GetHashCode(value);
            }
        }

        public static bool operator ==(Atom<T> left, Atom<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Atom<T> left, Atom<T> right)
        {
            return !left.Equals(right);
        }

        public int CompareTo(Atom<T> other)
        {
            return id.CompareTo(other.id);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            return obj is Atom<T> other
                ? CompareTo(other)
                : throw new ArgumentException($"Object must be of type {nameof(Atom<T>)}");
        }

        public static bool operator <(Atom<T> left, Atom<T> right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(Atom<T> left, Atom<T> right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(Atom<T> left, Atom<T> right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Atom<T> left, Atom<T> right)
        {
            return left.CompareTo(right) >= 0;
        }

        public sealed class IdRelationalComparer : IComparer<Atom<T>>, IComparer
        {
            public int Compare(Atom<T> x, Atom<T> y)
            {
                return Comparer<Ident>.Default.Compare(x.id, y.id);
            }

            public int Compare(object x, object y)
            {
                return Comparer<Ident>.Default.Compare(((Atom<T>) x).id, ((Atom<T>) y).id);
            }
        }

        public static IdRelationalComparer idComparer { get; } = new IdRelationalComparer();
    }
}