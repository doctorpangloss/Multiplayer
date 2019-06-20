using System;
using System.Threading;
using UniRx;
using UniRx.Async;

namespace HiddenSwitch
{
    public class ReactiveReadOnly<T> : IReadOnlyReactiveProperty<T>, IDisposable
    {
        private ReadOnlyReactiveProperty<T> m_Inner;

        public ReactiveReadOnly(IObservable<T> inner)
        {
            m_Inner = inner.ToReadOnlyReactiveProperty();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return m_Inner.Subscribe(observer);
        }

        public static implicit operator T(ReactiveReadOnly<T> d)
        {
            return d.m_Inner.Value;
        }

        public void Dispose()
        {
            m_Inner.Dispose();
        }

        public T Value => m_Inner.Value;

        public bool HasValue => m_Inner.HasValue;

        public UniTask<T> WaitUntilValueChangedAsync(CancellationToken cancellationToken)
        {
            return m_Inner.WaitUntilValueChangedAsync(cancellationToken);
        }
    }
}