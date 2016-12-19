using System;
using System.Collections.Generic;
using System.Linq;

namespace Skewwhiffy.Batcher.Extensions
{
    public static class CollectionExtensions
    {
        public static string Join<T>(this IEnumerable<T> source, string separator = ",")
        {
            return string.Join(separator, source);
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var c in collection)
            {
                action(c);
            }
        }

        public static IEnumerable<int> To(this int from, int upToInclusive)
        {
            return Enumerable.Range(from, upToInclusive - from + 1);
        }

        public static IEnumerable<T> PickOutDuplicates<T>(this IEnumerable<T> source)
        {
            return source
                .GroupBy(s => s)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
        }
    }
}
