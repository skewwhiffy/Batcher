using System;

namespace Skewwhiffy.Batcher.Extensions
{
    public static class FunctionalExtensions
    {
        public static TTo Pipe<TFrom, TTo>(this TFrom from, Func<TFrom, TTo> to)
        {
            return to(from);
        }
    }
}
