using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Skewwhiffy.Batcher.Extensions;
using Skewwhiffy.Batcher.Fluent;

namespace Skewwhiffy.Batcher.Tests.TestHelpers
{
    public static class BatcherExtensions
    {
        private const int MillisecondsToWait = 5000;
        public static async Task WaitUntilDone<T>(this IBatcher<T> batcher)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                if (batcher.IsDone)
                {
                    break;
                }
                if (stopwatch.ElapsedMilliseconds > MillisecondsToWait)
                {
                    Assert.Fail("Timout: shouldn't take this long");
                }
                await Task.Delay(100);
            }
        }

        public static async Task WaitUntilConstant<TFrom, TTo>(this TFrom source, Func<TFrom, TTo> get)
        {
            var stopwatch = Stopwatch.StartNew();
            TTo previous = default(TTo);
            while (true)
            {
                var current = get(source);
                if (current.Equals(previous))
                {
                    return;
                }
                if (stopwatch.ElapsedMilliseconds > MillisecondsToWait)
                {
                    Assert.Fail("Timout: shouldn't take this long");
                }
                previous = current;
                await Task.Delay(100);
            }
        }

        public static async Task WaitUntil<TFrom>(this TFrom source, Func<TFrom, bool> get, Func<TFrom, string> message = null)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                if (get(source))
                {
                    return;
                }
                if (stopwatch.ElapsedMilliseconds > MillisecondsToWait)
                {
                    Assert.Fail(message?.Invoke(source) ?? "Timout: shouldn't take this long");
                }
                await Task.Delay(100);
            }
        }

        public static string GetMessage<T>(this List<T> source)
        {
            return $"Processed item count: {source.Count}, duplicates: {source.PickOutDuplicates().Join()}, items: {source.OrderBy(s => s).Join()}";
        }
    }
}
