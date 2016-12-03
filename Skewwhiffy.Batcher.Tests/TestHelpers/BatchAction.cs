using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skewwhiffy.Batcher.Tests.TestHelpers
{
    public class BatchAction
    {
        private List<int> _start;
        private ConcurrentBag<int> _processed;
        private ConcurrentBag<string> _actual;
        private SingleThreadBatcher<int> _batcher;
        private Func<int, bool> _throwWhen;

        public BatchAction()
        {
            _throwWhen = i => false;
            _start = Enumerable.Range(0, 100).ToList();
            _processed = new ConcurrentBag<int>();
            _actual = new ConcurrentBag<string>();
        }

        public void InitializeBatcher(SynchronicityTestCase synchronicity)
        {
            _batcher = GetBatcher(synchronicity);
        }

        public void StartBatcher()
        {
            _batcher.Process(_start);
        }

        public void ThrowWhen(Func<int, bool> throwWhen)
        {
            _throwWhen = throwWhen;
        }

        public SingleThreadBatcher<int> Batcher => _batcher;

        public bool AllProcessed => ProcessedItemsCount == _start.Count;

        public int ProcessedItemsCount => _processed.Count;

        public List<int> ProcessedItems => _processed.ToList();

        public List<int> StartItems => _start.ToList();

        public async Task WaitUntilAllProcessed()
        {
            var stopwatch = Stopwatch.StartNew();
            var lastCount = 0;
            while (true)
            {
                if (stopwatch.ElapsedMilliseconds > 5000 && lastCount == _processed.Count)
                {
                    Assert.Fail("Timout: shouldn't take this long");
                }
                if (AllProcessed)
                {
                    break;
                }
                if (_processed.Count > lastCount)
                {
                    lastCount = _processed.Count;
                }
                await Task.Delay(500);
            }
        }

        private SingleThreadBatcher<int> GetBatcher(SynchronicityTestCase synchronicity)
        {
            switch (synchronicity)
            {
                case SynchronicityTestCase.Synchronous:
                    return new SingleThreadBatcher<int>((Action<int>)ActionSync);
                case SynchronicityTestCase.Asynchronous:
                    return new SingleThreadBatcher<int>(ActionAsync);
                default:
                    throw new NotImplementedException();
            }
        }

        private void ActionSync(int input)
        {
            _processed.Add(input);
            if (_throwWhen(input))
            {
                throw new InvalidOperationException(input.ToString());
            }
        }

        private async Task ActionAsync(int input)
        {
            await Task.FromResult(0);
            if (_throwWhen(input))
            {
                _processed.Add(input);
                throw new InvalidOperationException(input.ToString());
            }
            else
            {
                var result = (input * input).ToString();
                _actual.Add(result);
                _processed.Add(input);
            }
        }
    }
}
