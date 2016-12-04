using System;
using System.Collections.Generic;

namespace Skewwhiffy.Batcher
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
    }
}
