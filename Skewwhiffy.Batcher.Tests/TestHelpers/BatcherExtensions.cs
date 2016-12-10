using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Skewwhiffy.Batcher.Fluent;

namespace Skewwhiffy.Batcher.Tests.TestHelpers
{
    public static class BatcherExtensions
    {
        public static async Task WaitUntilDone<T>(this IBatcher<T> batcher)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                if (batcher.IsDone)
                {
                    break;
                }
                if (stopwatch.ElapsedMilliseconds > 10000)
                {
                    Assert.Fail("Timout: shouldn't take this long");
                }
                await Task.Delay(100);
            }
        }
    }
}
