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
    public class ChainBatcherTestSetup : ISetupBatchTests
    {
        private readonly List<int> _start;
        private readonly ConcurrentBag<int> _squared;
        private readonly ConcurrentBag<int> _convertedToString;
        private readonly ConcurrentBag<string> _results;
        private Func<int, bool> _throwWhenSquaring;
        private Func<int, bool> _throwWhenConvertingToString;
        private Func<string, bool> _throwWhenPuttingInResultsBag;

        public ChainBatcherTestSetup()
        {
            _squared = new ConcurrentBag<int>();
            _convertedToString = new ConcurrentBag<int>();
            _start = 10.To(109).ToList();
            _results = new ConcurrentBag<string>();
        }

        public TimeSpan? PauseBetweenProcessing { get; set; }

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
                    return TakeAnItem<int>
                    .Then(Square)
                    .Then(ConvertToStringAsync)
                    .AndFinally((Action<string>)PutInBag);
                case SynchronicityTestCase.Asynchronous:
                    return TakeAnItem<int>
                    .Then(SquareAsync)
                    .Then(ConvertToString)
                    .AndFinally(PutInBagAsync);
                default:
                    throw new NotImplementedException();
            }
        }

        public void ThrowWhenSquaring(Func<int, bool> throwWhen) => _throwWhenSquaring = throwWhen;

        public void ThrowWhenConvertingToString(Func<int, bool> throwWhen) => _throwWhenConvertingToString = throwWhen;

        public void ThrowWhenPuttingInResultsBag(Func<string, bool> throwWhen) => _throwWhenPuttingInResultsBag = throwWhen;

        public List<int> StartItems => _start.ToList();

        public List<int> ConvertedToString => _convertedToString.ToList();

        public List<string> Results => _results.ToList();

        public List<int> SquaredItems => _squared.ToList();

        public int ProcessedItemsCount => _results.Count;

        public List<int> ProcessedItems => _squared.ToList();

        private int Square(int input)
        {
            if (_throwWhenSquaring != null && _throwWhenSquaring(input))
            {
                throw new InvalidOperationException($"Squaring: {input}");
            }
            _squared.Add(input);
            if (PauseBetweenProcessing.HasValue)
            {
                Thread.Sleep(PauseBetweenProcessing.Value);
            }
            return input * input;
        }

        private async Task<int> SquareAsync(int input, CancellationToken token)
        {
            if (_throwWhenSquaring != null && _throwWhenSquaring(input))
            {
                throw new InvalidOperationException($"Squaring: {input}");
            }
            _squared.Add(input);
            if (PauseBetweenProcessing.HasValue)
            {
                await Task.Delay(PauseBetweenProcessing.Value, token);
            }
            return input * input;
        }

        private string ConvertToString(int input)
        {
            if (_throwWhenConvertingToString != null && _throwWhenConvertingToString(input))
            {
                throw new InvalidOperationException($"Converting to string: {input}");
            }
            _convertedToString.Add(input);
            if (PauseBetweenProcessing.HasValue)
            {
                Thread.Sleep(PauseBetweenProcessing.Value);
            }
            return input.ToString();
        }

        private async Task<string> ConvertToStringAsync(int input, CancellationToken token)
        {
            if (_throwWhenConvertingToString != null && _throwWhenConvertingToString(input))
            {
                throw new InvalidOperationException($"Converting to string: {input}");
            }
            _convertedToString.Add(input);
            if (PauseBetweenProcessing.HasValue)
            {
                await Task.Delay(PauseBetweenProcessing.Value, token);
            }
            return input.ToString();
        }

        private void PutInBag(string input)
        {
            if (_throwWhenPuttingInResultsBag != null && _throwWhenPuttingInResultsBag(input))
            {
                throw new InvalidOperationException($"Result: {input}");
            }
            if (PauseBetweenProcessing.HasValue)
            {
                Thread.Sleep(PauseBetweenProcessing.Value);
            }
            _results.Add(input);
        }

        private async Task PutInBagAsync(string input, CancellationToken token)
        {
            if (_throwWhenPuttingInResultsBag != null && _throwWhenPuttingInResultsBag(input))
            {
                throw new InvalidOperationException($"Result: {input}");
            }
            if (PauseBetweenProcessing.HasValue)
            {
                await Task.Delay(PauseBetweenProcessing.Value, token);
            }
            _results.Add(input);
        }
    }
}
