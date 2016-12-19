using System;
using System.Collections.Generic;
using System.Linq;

namespace Skewwhiffy.Batcher.Extensions
{
    public static class FunctionalExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach(var c in collection)
            {
                action(c);
            }
        }

        public static IEnumerable<int> To(this int from, int upToInclusive)
        {
            return Enumerable.Range(from, upToInclusive - from + 1);
        }
    }
}
