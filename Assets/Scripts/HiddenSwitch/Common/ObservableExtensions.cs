using System;
using UniRx;

namespace HiddenSwitch
{
    public static class ObservableExtensions
    {
        public static IObservable<T> ToObservableAndAdded<T>(this IReadOnlyReactiveCollection<T> collection)
        {
            return collection.ToObservable()
                .Merge(collection
                    .ObserveAdd()
                    .Select(added => added.Value));
        }

        public static IObservable<T> ToObservableAddedAndReplaced<T>(this IReadOnlyReactiveCollection<T> collection)
        {
            return collection.ToObservable()
                .Merge(collection
                        .ObserveAdd()
                        .Select(added => added.Value),
                    collection.ObserveReplace()
                        .Select(replaced => replaced.NewValue));
        }
    }
}