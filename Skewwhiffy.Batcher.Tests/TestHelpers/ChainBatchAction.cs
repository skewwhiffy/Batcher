using NUnit.Framework;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Skewwhiffy.Batcher.Tests.TestHelpers
{
    public class ChainBatchAction
    {
        private List<int> _start;
        private ConcurrentBag<int> _squared;
        private ConcurrentBag<int> _convertedToString;
        private ConcurrentBag<string> _results;
        private ChainBatcher<int> _batcher;

        public ChainBatchAction()
        {
            _squared = new ConcurrentBag<int>();
            _convertedToString = new ConcurrentBag<int>();
            _start = Enumerable.Range(0, 100).ToList();
            _results = new ConcurrentBag<string>();
        }

        public void InitializeBatcher()
        {
            _batcher = new ChainBatcher<int>(Square, ConvertToString, PutInBag);
        }

        public void StartBatcher()
        {
            _batcher.Process(_start);
        }

        public int ProcessedItemsCount => _convertedToString.Count;

        public bool AllProcessed => ProcessedItemsCount == _start.Count;

        public async Task WaitUntilAllProcessed()
        {
            var stopwatch = Stopwatch.StartNew();
            var lastCount = 0;
            while (true)
            {
                if (stopwatch.ElapsedMilliseconds > 5000 && lastCount == _convertedToString.Count)
                {
                    Assert.Fail("Timout: shouldn't take this long");
                }
                if (AllProcessed)
                {
                    break;
                }
                if (_convertedToString.Count > lastCount)
                {
                    lastCount = _convertedToString.Count;
                }
                await Task.Delay(500);
            }
        }

        private int Square(int input) => input * input;

        private Task<string> ConvertToString(int input)
        {
            _convertedToString.Add(input);
            return Task.FromResult(input.ToString());
        }

        private void PutInBag(string input)
        {
            _results.Add(input);
        }
    }
}
