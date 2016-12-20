using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Skewwhiffy.Batcher.Impl;

namespace Skewwhiffy.Batcher.Fluent
{
    internal class ChainBuilder<TIn, TOut> : IBatchChainBuilder<TIn, TOut>
    {
        private SingleTaskBatcher<TIn> _firstBatcher;
        private Func<SingleTaskBatcher<TOut>> _nextBatcher;
        private List<Func<IEnumerable<Exception>>> _getExceptions;
        private List<SingleTaskBatcher> _batchers;

        internal ChainBuilder(Func<TIn, TOut> func)
        {
            _firstBatcher = new SingleTaskBatcher<TIn>(i =>
            {
                var result = func(i);
                _nextBatcher().Process(result);
            });
            Init(_firstBatcher);
        }

        internal ChainBuilder(Func<TIn, Task<TOut>> func)
        {
            _firstBatcher = new SingleTaskBatcher<TIn>(async i =>
            {
                var result = await func(i);
                _nextBatcher().Process(result);
            });
            Init(_firstBatcher);
        }

        internal ChainBuilder(Func<TIn, CancellationToken, Task<TOut>> func)
        {
            _firstBatcher = new SingleTaskBatcher<TIn>(async (i, t) =>
            {
                var result = await func(i, t);
                _nextBatcher().Process(result);
            });
            Init(_firstBatcher);
        }

        private ChainBuilder(
            SingleTaskBatcher<TIn> firstBatcher,
            List<Func<IEnumerable<Exception>>> getExceptions,
            List<SingleTaskBatcher> batchers)
        {
            Init(firstBatcher, getExceptions, batchers);
        }

        private void Init(
            SingleTaskBatcher<TIn> firstBatcher,
            List<Func<IEnumerable<Exception>>> getExceptions = null,
            List<SingleTaskBatcher> batchers = null)
        {
            _firstBatcher = firstBatcher;
            _getExceptions = getExceptions ?? new List<Func<IEnumerable<Exception>>>
            {
                () => firstBatcher.Exceptions
            };
            _batchers = batchers ?? new List<SingleTaskBatcher>
            {
                firstBatcher
            };
        }

        public IBatchChainBuilder<TIn, TOut> WithThreads(int threadCount)
        {
            _batchers.Last().WithThreads(threadCount);
            return this;
        }

        public IBatchChainBuilder<TIn, TNewOut> Then<TNewOut>(Func<TOut, Task<TNewOut>> func)
        {
            return ThenInternal<TNewOut>(builder => new SingleTaskBatcher<TOut>(async i =>
            {
                var result = await func(i);
                builder._nextBatcher().Process(result);
            }));
        }

        public IBatchChainBuilder<TIn, TNewOut> Then<TNewOut>(Func<TOut, CancellationToken, Task<TNewOut>> func)
        {
            return ThenInternal<TNewOut>(builder => new SingleTaskBatcher<TOut>(async (i, t) =>
            {
                var result = await func(i, t);
                builder._nextBatcher().Process(result);
            }));
        }

        public IBatchChainBuilder<TIn, TNewOut> Then<TNewOut>(Func<TOut, TNewOut> func)
        {
            return ThenInternal<TNewOut>(builder => new SingleTaskBatcher<TOut>(i =>
            {
                var result = func(i);
                builder._nextBatcher().Process(result);
            }));
        }

        public IBatcher<TIn> AndFinally(Action<TOut> action) => AndFinally(new SingleTaskBatcher<TOut>(action));

        public IBatcher<TIn> AndFinally(Func<TOut, Task> action) => AndFinally(new SingleTaskBatcher<TOut>(action));

        public IBatcher<TIn> AndFinally(Func<TOut, CancellationToken, Task> action) => AndFinally(new SingleTaskBatcher<TOut>(action));

        private IBatchChainBuilder<TIn, TNewOut> ThenInternal<TNewOut>(Func<ChainBuilder<TIn, TNewOut>, SingleTaskBatcher<TOut>> getBatcher)
        {
            var builder = new ChainBuilder<TIn, TNewOut>(_firstBatcher, _getExceptions, _batchers);
            var nextBatcher = getBatcher(builder);
            _nextBatcher = () => nextBatcher;
            _getExceptions.Add(() => nextBatcher.Exceptions);
            _batchers.Add(nextBatcher);
            return builder;
        }

        private ChainBatcher<TIn> AndFinally(SingleTaskBatcher<TOut> finalBatcher)
        {
            _nextBatcher = () => finalBatcher;
            _getExceptions.Add(() => finalBatcher.Exceptions);
            _batchers.Add(finalBatcher);
            return new ChainBatcher<TIn>(_firstBatcher, _getExceptions, _batchers);
        }
    }
}
