using NUnit.Framework;
using System;
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
        private Func<int, bool> _throwWhenSquaring;
        private Func<int, bool> _throwWhenConvertingToString;
        private Func<string, bool> _throwWhenPuttingInResultsBag;

        public ChainBatchAction()
        {
            _squared = new ConcurrentBag<int>();
            _convertedToString = new ConcurrentBag<int>();
            _start = Enumerable.Range(0, 100).ToList();
            _results = new ConcurrentBag<string>();
        }

        public ChainBatcher<int> Batcher => _batcher;

        public void InitializeBatcherStartingWith(SynchronicityTestCase synchronicity)
        {
            _batcher = GetBatcher(synchronicity);
        }

        private ChainBatcher<int> GetBatcher(SynchronicityTestCase synchronicity)
        {
            switch (synchronicity)
            {
                case SynchronicityTestCase.Synchronous:
                    return TakeAnItem
                    .Then<int, int>(i => Square(i))
                    .Then(i => ConvertToStringAsync(i))
                    .AndFinally(s => PutInBag(s));
                case SynchronicityTestCase.Asynchronous:
                    return TakeAnItem
                    .Then<int, int>(i => SquareAsync(i))
                    .Then(i => ConvertToString(i))
                    .AndFinally(PutInBagAsync);
                default:
                    throw new NotImplementedException();
            }
        }

        public void StartBatcher() => Batcher.Process(_start);

        public void ThrowWhenSquaring(Func<int, bool> throwWhen) => _throwWhenSquaring = throwWhen;

        public void ThrowWhenConvertingToString(Func<int, bool> throwWhen) => _throwWhenConvertingToString = throwWhen;

        public void ThrowWhenPuttingInResultsBag(Func<string, bool> throwWhen) => _throwWhenPuttingInResultsBag = throwWhen;

        public List<int> StartItems => _start.ToList();

        public List<int> ConvertedToString => _convertedToString.ToList();

        public List<string> Results => _results.ToList();

        public List<int> SquaredItems => _squared.ToList();

        public int ProcessedItemsCount => _results.Count;

        public bool AllProcessed => ProcessedItemsCount == _start.Count;

        public async Task WaitUntilAllProcessed()
        {
            var stopwatch = Stopwatch.StartNew();
            var lastCount = -1;
            while (true)
            {
                if (stopwatch.ElapsedMilliseconds > 5000)
                {
                    Assert.Fail("Timout: shouldn't take this long");
                }
                if (lastCount == _convertedToString.Count)
                {
                    break;
                }
                if (_convertedToString.Count > lastCount)
                {
                    lastCount = _convertedToString.Count;
                }
                await Task.Delay(1000);
            }
        }

        private Task<int> SquareAsync(int input)
        {
            return Task.FromResult(Square(input));
        }

        private int Square(int input)
        {
            if (_throwWhenSquaring != null && _throwWhenSquaring(input))
            {
                throw new InvalidOperationException($"Squaring: {input}");
            }
            _squared.Add(input);
            return input * input;
        }

        private string ConvertToString(int input)
        {

            if (_throwWhenConvertingToString != null && _throwWhenConvertingToString(input))
            {
                throw new InvalidOperationException($"Converting to string: {input}");
            }
            _convertedToString.Add(input);
            return input.ToString();
        }

        private Task<string> ConvertToStringAsync(int input)
        {
            return Task.FromResult(ConvertToString(input));
        }

        private void PutInBag(string input)
        {
            if (_throwWhenPuttingInResultsBag != null && _throwWhenPuttingInResultsBag(input))
            {
                throw new InvalidOperationException($"Result: {input}");
            }
            _results.Add(input);
        }

        private Task PutInBagAsync(string input)
        {
            PutInBag(input);
            return Task.FromResult(0);
        }
    }
}
