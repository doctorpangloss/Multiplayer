using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UniRx;

namespace HiddenSwitch.Networking.Peers.Internal
{
    public class KSEQReplicatedList<T> : IReactiveCollection<T>
    {
        protected IList<Atom<T>> m_AtomList = new List<Atom<T>>();
        public string replicaId { get; }
        protected long time { get; set; }
        protected ISet<Ident> m_Removed;
        protected bool isDisposed;
        internal LSEQIdentGenerator m_IdentGenerator;

        protected int Add(Ident id, T value)
        {
            var pos = BisectRight(id);
            var hasExisting = pos - 1 >= 0 && pos - 1 < Count;
            int index = pos - 1;
            if (hasExisting && m_AtomList[index].id.CompareTo(id) == 0)
            {
                return -1;
            }

            var atom = new Atom<T>(id, value);
            m_AtomList.Insert(pos, atom);
            return pos;
        }


        protected int BisectLeft(Ident id)
        {
            var min = 0;
            var max = m_AtomList.Count;
            while (min < max)
            {
                var curr = (min + max) / 2;
                if (m_AtomList[curr].id.CompareTo(id) < 0)
                {
                    min = curr + 1;
                }
                else
                {
                    max = curr;
                }
            }

            return min;
        }

        protected int BisectRight(Ident id)
        {
            var min = 0;
            var max = m_AtomList.Count;

            while (min < max)
            {
                var curr = (min + max) / 2;
                if (id.CompareTo(m_AtomList[curr].id) < 0)
                {
                    max = curr;
                }
                else
                {
                    min = curr + 1;
                }
            }

            return min;
        }

        protected int Remove(Ident id)
        {
            var pos = IndexOf(id);
            if (pos >= 0)
            {
                m_AtomList.RemoveAt(pos);
                return pos;
            }

            return -1;
        }

        protected int IndexOf(Ident id)
        {
            var pos = BisectLeft(id);
            if (pos != m_AtomList.Count && m_AtomList[pos].id.CompareTo(id) == 0)
            {
                return pos;
            }

            return -1;
        }

        public KSEQReplicatedList(string replicaId, IList<Atom<T>> atomList = null)
        {
            this.replicaId = replicaId;
            time = 0;
            m_AtomList = atomList ?? new List<Atom<T>>();
            m_Removed = new HashSet<Ident>();
            m_IdentGenerator = new LSEQIdentGenerator();
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            foreach (var atom in m_AtomList)
            {
                yield return atom.value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            Insert(Count, item);
        }

        public virtual void Clear()
        {
            throw new NotSupportedException();
        }

        public virtual bool Contains(T item)
        {
            return m_AtomList.Any(atom => Equals(atom.value, item));
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            for (var i = 0; i < Math.Min(array.Length - arrayIndex, Count); i++)
            {
                array[arrayIndex + i] = m_AtomList[i].value;
            }
        }

        public virtual bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index == -1)
            {
                return false;
            }

            RemoveAt(index);
            return true;
        }

        int IReactiveCollection<T>.Count => Count;

        public virtual int Count => m_AtomList.Count;

        public bool IsReadOnly => false;

        public virtual int IndexOf(T item)
        {
            for (var i = 0; i < m_AtomList.Count; i++)
            {
                var t = m_AtomList[i];
                if (Equals(t.value, item))
                {
                    return i;
                }
            }

            return -1;
        }

        public KSEQOperation<T>? lastOp { get; protected set; }

        public void Insert(int index, T item)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    "The position must be greater than or equal to zero");
            }

            BeforeInsertItem(ref item);

            var beforeId = index - 1 < m_AtomList.Count && index - 1 >= 0 ? m_AtomList[index - 1].id : null;
            var afterId = index < m_AtomList.Count && index >= 0 ? m_AtomList[index].id : null;
            var id = m_IdentGenerator.GetIdent(replicaId, ++time, beforeId, afterId);
            var op = new KSEQOperation<T>
            {
                id = id,
                op = KSEQOperationTypes.Insert,
                realTime = GetWallTime(),
                value = item,
                replicaId = replicaId
            };
            Apply(op);
            lastOp = op;
        }

        protected virtual void BeforeInsertItem(ref T item)
        {
        }

        protected long GetWallTime()
        {
            return DateTime.UtcNow.Ticks;
        }

        public void Apply(KSEQOperation<T>? op)
        {
            if (op == null)
            {
                return;
            }

            Apply(op.Value);
        }

        public struct ApplicationResult
        {
            public bool applied { get; set; }
            public int index { get; set; }
        }

        public virtual ApplicationResult Apply(KSEQOperation<T> op, bool quiet = false)
        {
            switch (op.op)
            {
                case KSEQOperationTypes.Insert:
                    if (m_Removed.Contains(op.id))
                    {
                        break;
                    }

                    var indexAdded = Add(op.id, op.value);
                    if (!quiet)
                    {
                        var collectionAddEvent = new CollectionAddEvent<T>(indexAdded, op.value);
                        collectionAdd?.OnNext(collectionAddEvent);
                        countChanged?.OnNext(m_AtomList.Count);
                        if (op.replicaId == replicaId)
                        {
                            collectionThisReplicaAdded?.OnNext(collectionAddEvent);
                        }
                    }

                    return new ApplicationResult()
                    {
                        applied = true,
                        index = indexAdded
                    };

                case KSEQOperationTypes.Remove:
                    if (m_Removed.Contains(op.id))
                    {
                        break;
                    }

                    m_Removed.Add(op.id);
                    var indexRemoved = IndexOf(op.id);
                    if (indexRemoved < 0)
                    {
                        break;
                    }

                    var oldValue = m_AtomList[indexRemoved];
                    m_AtomList.RemoveAt(indexRemoved);
                    if (!quiet)
                    {
                        var collectionRemoveEvent = new CollectionRemoveEvent<T>(indexRemoved, oldValue.value);
                        collectionRemove?.OnNext(collectionRemoveEvent);
                        countChanged?.OnNext(m_AtomList.Count);
                        if (op.replicaId == replicaId)
                        {
                            collectionThisReplicaRemoved?.OnNext(collectionRemoveEvent);
                        }
                    }

                    return new ApplicationResult()
                    {
                        applied = true,
                        index = indexRemoved
                    };
            }

            return new ApplicationResult()
            {
            };
        }

        public virtual void RemoveAt(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    "The position must be greater than or equal to zero.");
            }

            if (index >= m_AtomList.Count)
            {
                lastOp = null;
                return;
            }

            BeforeRemoveItem(index);

            var op = new KSEQOperation<T>
            {
                replicaId = replicaId,
                realTime = GetWallTime(),
                op = KSEQOperationTypes.Remove,
                id = m_AtomList[index].id
            };

            Apply(op);
            lastOp = op;
        }

        protected virtual void BeforeRemoveItem(int index)
        {
        }

        int IReadOnlyReactiveCollection<T>.Count => Count;

        public virtual T this[int index]
        {
            get => m_AtomList[index].value;
            set => throw new NotSupportedException();
        }

        public virtual void Move(int oldIndex, int newIndex)
        {
            throw new NotSupportedException();
        }

        [NonSerialized] protected Subject<int> countChanged;

        public IObservable<int> ObserveCountChanged(bool notifyCurrentCount = false)
        {
            if (isDisposed) return Observable.Empty<int>();

            var subject = countChanged ?? (countChanged = new Subject<int>());
            if (notifyCurrentCount)
            {
                return subject.StartWith(() => Count);
            }

            return subject.ObserveOnMainThread();
        }

        [NonSerialized] protected Subject<Unit> collectionReset;

        public IObservable<Unit> ObserveReset()
        {
            if (isDisposed) return Observable.Empty<Unit>();
            return (collectionReset ?? (collectionReset = new Subject<Unit>())).ObserveOnMainThread();
        }

        [NonSerialized] protected Subject<CollectionAddEvent<T>> collectionAdd;

        public IObservable<CollectionAddEvent<T>> ObserveAdd()
        {
            if (isDisposed) return Observable.Empty<CollectionAddEvent<T>>();
            return (collectionAdd ?? (collectionAdd = new Subject<CollectionAddEvent<T>>())).ObserveOnMainThread();
        }

        [NonSerialized] protected Subject<CollectionMoveEvent<T>> collectionMove;

        public IObservable<CollectionMoveEvent<T>> ObserveMove()
        {
            if (isDisposed) return Observable.Empty<CollectionMoveEvent<T>>();
            return (collectionMove ?? (collectionMove = new Subject<CollectionMoveEvent<T>>())).ObserveOnMainThread();
        }

        [NonSerialized] protected Subject<CollectionRemoveEvent<T>> collectionRemove;

        public IObservable<CollectionRemoveEvent<T>> ObserveRemove()
        {
            if (isDisposed) return Observable.Empty<CollectionRemoveEvent<T>>();
            return (collectionRemove ?? (collectionRemove = new Subject<CollectionRemoveEvent<T>>()))
                .ObserveOnMainThread();
        }

        [NonSerialized] protected Subject<CollectionReplaceEvent<T>> collectionReplace;

        public IObservable<CollectionReplaceEvent<T>> ObserveReplace()
        {
            if (isDisposed) return Observable.Empty<CollectionReplaceEvent<T>>();
            return (collectionReplace ?? (collectionReplace = new Subject<CollectionReplaceEvent<T>>()))
                .ObserveOnMainThread();
        }

        [NonSerialized] protected Subject<CollectionAddEvent<T>> collectionThisReplicaAdded;

        public IObservable<CollectionAddEvent<T>> ObserveThisReplicaAdded()
        {
            if (isDisposed) return Observable.Empty<CollectionAddEvent<T>>();
            return (collectionThisReplicaAdded ?? (collectionThisReplicaAdded = new Subject<CollectionAddEvent<T>>()))
                .ObserveOnMainThread();
        }


        [NonSerialized] protected Subject<CollectionRemoveEvent<T>> collectionThisReplicaRemoved;

        public IObservable<CollectionRemoveEvent<T>> ObserveThisReplicaRemoved()
        {
            if (isDisposed) return Observable.Empty<CollectionRemoveEvent<T>>();
            return (collectionThisReplicaRemoved ??
                    (collectionThisReplicaRemoved = new Subject<CollectionRemoveEvent<T>>())).ObserveOnMainThread();
        }

        [NonSerialized] protected Subject<CollectionReplaceEvent<T>> collectionThisReplicaReplaced;

        public IObservable<CollectionReplaceEvent<T>> ObserveThisReplicaReplaced()
        {
            if (isDisposed) return Observable.Empty<CollectionReplaceEvent<T>>();
            return (collectionThisReplicaReplaced ??
                   (collectionThisReplicaReplaced = new Subject<CollectionReplaceEvent<T>>())).ObserveOnMainThread();
        }


        void DisposeSubject<TSubject>(ref Subject<TSubject> subject)
        {
            if (subject != null)
            {
                try
                {
                    subject.OnCompleted();
                }
                finally
                {
                    subject.Dispose();
                    subject = null;
                }
            }
        }

        #region IDisposable Support

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DisposeSubject(ref collectionReset);
                    DisposeSubject(ref collectionAdd);
                    DisposeSubject(ref collectionMove);
                    DisposeSubject(ref collectionRemove);
                    DisposeSubject(ref collectionReplace);
                    DisposeSubject(ref countChanged);
                    DisposeSubject(ref collectionThisReplicaAdded);
                    DisposeSubject(ref collectionThisReplicaRemoved);
                    DisposeSubject(ref collectionThisReplicaReplaced);
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}