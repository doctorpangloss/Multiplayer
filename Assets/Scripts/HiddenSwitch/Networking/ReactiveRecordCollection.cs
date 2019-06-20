using System.Threading;
using UniRx;

namespace HiddenSwitch.Networking
{
    public class ReactiveRecordCollection : ReactiveCollection<Record>, IReactiveRecordCollection<Record>
    {
        private int m_CurrentId = 0;

        protected override void InsertItem(int index, Record item)
        {
            SetId(ref item);
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, Record item)
        {
            SetId(ref item);
            base.SetItem(index, item);
        }

        protected void SetId(ref Record record)
        {
            if (record.id == 0)
            {
                record.id = Interlocked.Increment(ref m_CurrentId);
            }
        }

        public bool Replace(Record replacement)
        {
            for (var i = 0; i < Count; i++)
            {
                if (this[i].id == replacement.id)
                {
                    this[i] = replacement;
                    return true;
                }
            }

            return false;
        }
    }
}