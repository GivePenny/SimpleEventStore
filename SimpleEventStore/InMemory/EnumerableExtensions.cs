using System;
using System.Collections.Generic;

namespace SimpleEventStore.InMemory
{
    static class EnumerableExtensions
    {
        public static IEnumerable<T> TakeUntilImmediatelyAfter<T>(this IEnumerable<T> source, Predicate<T> stopImmediatelyAfter)
        {
            foreach (var item in source)
            {
                yield return item;

                if (stopImmediatelyAfter(item))
                {
                    yield break;
                }
            }
        }
    }
}
