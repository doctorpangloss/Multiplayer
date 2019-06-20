using System;
using System.Collections.Generic;
using System.Threading;
using UniRx;

namespace HiddenSwitch.Networking.Peers.Internal
{
    public class ReplicatedReactiveRecordCollection<T> : KSEQReplicatedList<T>, IReactiveRecordCollection<T>
        where T : IId, new()
    {
        private int m_CurrentId = 0;
        private HashSet<int> m_RemovedIds = new HashSet<int>();
        private Dictionary<int, int> m_IdToIndex = new Dictionary<int, int>();
        private Dictionary<int, KSEQOperation<T>> m_QueuedSet = new Dictionary<int, KSEQOperation<T>>();

        public IList<Atom<T>> atoms => m_AtomList;

        public ReplicatedReactiveRecordCollection(string replicaId,
            IList<Atom<T>> atomList = null /*TODO: Need removed Ids!*/) : base(replicaId,
            atomList)
        {
            if (atomList == null)
            {
                return;
            }

            for (var i = 0; i < atomList.Count; i++)
            {
                m_IdToIndex[atomList[i].value.id] = i;
            }
        }

        public T CreateRecord()
        {
            var t = new T();
            SetId(ref t);
            return t;
        }


        public bool SetId(ref T record)
        {
            if (record.id != 0)
            {
                return false;
            }

            var id = Interlocked.Increment(ref m_CurrentId);
            var hash = replicaId.GetHashCode() << 16 | id;

            record.id = hash;
            return true;
        }

        protected override void BeforeInsertItem(ref T item)
        {
            SetId(ref item);
            base.BeforeInsertItem(ref item);
        }

        public override T this[int index]
        {
            get => base[index];
            set
            {
                SetId(ref value);
                var beforeId = index - 1 < m_AtomList.Count && index - 1 >= 0 ? m_AtomList[index - 1].id : null;
                var afterId = index < m_AtomList.Count && index >= 0 ? m_AtomList[index].id : null;
                var id = m_IdentGenerator.GetIdent(replicaId, ++time, beforeId, afterId);
                var op = new KSEQOperation<T>
                {
                    id = id,
                    op = KSEQOperationTypes.Set,
                    realTime = GetWallTime(),
                    replicaId = replicaId,
                    value = value
                };
                lastOp = op;
                Apply(op, false);
            }
        }

        public bool Replace(T value)
        {
            if (!m_IdToIndex.ContainsKey(value.id))
            {
                throw new ArgumentException("Cannot replace a record that is not in this collection", nameof(value));
            }

            this[m_IdToIndex[value.id]] = value;
            return true;
        }

        public override ApplicationResult Apply(KSEQOperation<T> op, bool quiet = false)
        {
            // Retrieve the newest queued set for the given ID if the item already exists, performing an insert with its
            // id iif the id is later, and using the appropriate value.
            if (op.op == KSEQOperationTypes.Insert
                && m_QueuedSet.ContainsKey(op.value.id))
            {
                var replacementOp = m_QueuedSet[op.value.id];
                if (op.id < replacementOp.id)
                {
                    throw new ArgumentException(
                        $"op {op} has an ident with precedence over a set but matching id {op.value.id}", nameof(op));
                }

                m_QueuedSet.Remove(op.value.id);
                op.value = replacementOp.value;
                op.id = replacementOp.id;
            }

            var handled = base.Apply(op, quiet);
            if (handled.applied)
            {
                switch (op.op)
                {
                    // Worst case: O(N) (op to the beginning of the list
                    // Best case: O(1) (op to the end of the list)
                    case KSEQOperationTypes.Insert:
                        m_IdToIndex[op.value.id] = handled.index;
                        for (var i = handled.index + 1; i < Count; i++)
                        {
                            m_IdToIndex[m_AtomList[i].value.id] = i;
                        }

                        break;
                    case KSEQOperationTypes.Remove:
                        m_IdToIndex.Remove(op.value.id);
                        m_RemovedIds.Remove(op.value.id);
                        for (var i = handled.index; i < Count; i++)
                        {
                            m_IdToIndex[m_AtomList[i].value.id] = i;
                        }

                        break;
                }

                return handled;
            }

            switch (op.op)
            {
                case KSEQOperationTypes.Set:
                    if (m_RemovedIds.Contains(op.value.id) || m_Removed.Contains(op.id))
                    {
                        break;
                    }

                    // Find an existing record. Otherwise, queue up
                    if (m_IdToIndex.ContainsKey(op.value.id))
                    {
                        var i = m_IdToIndex[op.value.id];
                        var atom = m_AtomList[i];
                        var originalValue = atom.value;
                        if (originalValue.id != op.value.id)
                        {
                            throw new ArgumentException($"invalid set, {op.value.id}!={originalValue.id}");
                        }

                        // If this is later (last write first), go ahead and replace the atom with the new value.
                        // Otherwise, do nothing.
                        if (op.id < atom.id)
                        {
                            var oldAtom = atom;
                            atom.id = op.id;
                            atom.value = op.value;
                            Remove(oldAtom.id);
                            var j = Add(op.id, op.value);
                            for (var k = Math.Min(i, j); k <= Math.Max(i, j); k++)
                            {
                                m_IdToIndex[m_AtomList[k].value.id] = k;
                            }

                            if (!quiet)
                            {
                                var collectionReplaceEvent = new CollectionReplaceEvent<T>(i, originalValue, op.value);
                                collectionReplace?.OnNext(
                                    collectionReplaceEvent);

                                if (i != j)
                                {
                                    collectionMove?.OnNext(new CollectionMoveEvent<T>(i, j, op.value));
                                }

                                if (op.replicaId == replicaId)
                                {
                                    collectionThisReplicaReplaced?.OnNext(collectionReplaceEvent);
                                }
                            }
                        }


                        return new ApplicationResult()
                        {
                            applied = true,
                            index = i
                        };
                    }
                    else
                    {
                        if (!m_QueuedSet.ContainsKey(op.value.id)
                            || (m_QueuedSet.ContainsKey(op.value.id) && op.id < m_QueuedSet[op.value.id].id))
                        {
                            // Only replace the existing queued set if this op is later
                            m_QueuedSet[op.value.id] = op;
                        }

                        return new ApplicationResult()
                        {
                            applied = true
                        };
                    }
            }

            return new ApplicationResult();
        }
    }
}