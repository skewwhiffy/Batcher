using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Skewwhiffy.Batcher.Extensions;
using Skewwhiffy.Batcher.Fluent;

namespace Skewwhiffy.Batcher.Tests.TestHelpers
{
    public class SingleTaskBatcherTestSetup : ISetupBatchTests
    {
        private readonly List<int> _start;
        private readonly ConcurrentBag<int> _processed;

        public SingleTaskBatcherTestSetup()
        {
            ThrowWhen = i => false;
            _start = 1.To(100).ToList();
            _processed = new ConcurrentBag<int>();
        }

        public Func<int, bool> ThrowWhen { get; set; }

        public TimeSpan? PauseBetweenProcessing { get; set; }

        public bool AllProcessed => ProcessedItemsCount == _start.Count;

        public int ProcessedItemsCount => _processed.Count;

        public List<int> ProcessedItems => _processed.ToList();

        public List<int> StartItems => _start.ToList();

        public IBatcher<int> GetBatcher(SynchronicityTestCase synchronicity, ParallelMultiplicity multiplicity)
        {
            var batcher = GetBatcher(synchronicity);
            if (multiplicity == ParallelMultiplicity.MultiThreaded)
            {
                batcher.WithThreads(5);
            }
            return batcher;
        }

        public IBatcher<int> GetBatcher(SynchronicityTestCase synchronicity)
        {
            switch (synchronicity)
            {
                case SynchronicityTestCase.Synchronous:
                    return TakeAnItem<int>.And((Action<int>)ActionSync);
                case SynchronicityTestCase.Asynchronous:
                    return TakeAnItem<int>.And(ActionAsync);
                default:
                    throw new NotImplementedException();
            }
        }

        private void ActionSync(int input)
        {
            _processed.Add(input);
            if (ThrowWhen(input))
            {
                throw new InvalidOperationException(input.ToString());
            }
            if (PauseBetweenProcessing.HasValue)
            {
                Thread.Sleep(PauseBetweenProcessing.Value);
            }
        }

        private async Task ActionAsync(int input, CancellationToken token)
        {
            if (ThrowWhen(input))
            {
                _processed.Add(input);
                throw new InvalidOperationException(input.ToString());
            }
            _processed.Add(input);
            if (PauseBetweenProcessing.HasValue)
            {
                await Task.Delay(PauseBetweenProcessing.Value, token);
            }
        }
    }
}
