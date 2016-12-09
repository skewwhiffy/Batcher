using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skewwhiffy.Batcher
{
    public class TakeAnItem
    {
        public static ChainBuilder<TIn, TOut> Then<TIn, TOut>(Func<TIn, TOut> func)
        {
            return new ChainBuilder<TIn, TOut>(func);
        }
        public static ChainBuilder<TIn, TOut> Then<TIn, TOut>(Func<TIn, Task<TOut>> func)
        {
            return new ChainBuilder<TIn, TOut>(func);
        }
    }

    public class ChainBuilder<TIn, TOut>
    {
        private SingleThreadBatcher<TIn> _firstBatcher;
        private Func<SingleThreadBatcher<TOut>> _nextBatcher;
        private List<Func<IEnumerable<Exception>>> _getExceptions;
        private List<SingleThreadBatcher> _batchers;

        public ChainBuilder(Func<TIn, TOut> func)
        {
            _firstBatcher = new SingleThreadBatcher<TIn>(i =>
            {
                var result = func(i);
                _nextBatcher().Process(result);
            });
            Init(_firstBatcher);
        }

        public ChainBuilder(Func<TIn, Task<TOut>> func)
        {
            _firstBatcher = new SingleThreadBatcher<TIn>(async i =>
            {
                var result = await func(i);
                _nextBatcher().Process(result);
            });
            Init(_firstBatcher);
        }

        private ChainBuilder(
            SingleThreadBatcher<TIn> firstBatcher,
            List<Func<IEnumerable<Exception>>> getExceptions,
            List<SingleThreadBatcher> batchers)
        {
            Init(firstBatcher, getExceptions, batchers);
        }

        private void Init(
            SingleThreadBatcher<TIn> firstBatcher,
            List<Func<IEnumerable<Exception>>> getExceptions = null,
            List<SingleThreadBatcher> batchers = null)
        {
            _firstBatcher = firstBatcher;
            _getExceptions = getExceptions ?? new List<Func<IEnumerable<Exception>>>
            {
                () => firstBatcher.Exceptions
            };
            _batchers = batchers ?? new List<SingleThreadBatcher>
            {
                firstBatcher
            };
        }

        public ChainBuilder<TIn, TNewOut> Then<TNewOut>(Func<TOut, Task<TNewOut>> func)
        {
            var builder = new ChainBuilder<TIn, TNewOut>(_firstBatcher, _getExceptions, _batchers);
            var nextBatcher = new SingleThreadBatcher<TOut>(async i =>
            {
                var result = await func(i);
                builder._nextBatcher().Process(result);
            });
            _nextBatcher = () => nextBatcher;
            _getExceptions.Add(() => nextBatcher.Exceptions);
            _batchers.Add(nextBatcher);
            return builder;
        }

        public ChainBuilder<TIn, TNewOut> Then<TNewOut>(Func<TOut, TNewOut> func)
        {
            var builder = new ChainBuilder<TIn, TNewOut>(_firstBatcher, _getExceptions, _batchers);
            var nextBatcher = new SingleThreadBatcher<TOut>(i =>
            {
                var result = func(i);
                builder._nextBatcher().Process(result);
            });
            _nextBatcher = () => nextBatcher;
            _getExceptions.Add(() => nextBatcher.Exceptions);
            _batchers.Add(nextBatcher);
            return builder;
        }

        public ChainBatcher<TIn> AndFinally(Action<TOut> action)
        {
            var finalBatcher = new SingleThreadBatcher<TOut>(action);
            _nextBatcher = () => finalBatcher;
            _getExceptions.Add(() => finalBatcher.Exceptions);
            _batchers.Add(finalBatcher);
            return new ChainBatcher<TIn>(_firstBatcher, _getExceptions, _batchers);
        }

        public ChainBatcher<TIn> AndFinally(Func<TOut, Task> action)
        {
            var finalBatcher = new SingleThreadBatcher<TOut>(action);
            _nextBatcher = () => finalBatcher;
            _getExceptions.Add(() => finalBatcher.Exceptions);
            _batchers.Add(finalBatcher);
            return new ChainBatcher<TIn>(_firstBatcher, _getExceptions, _batchers);
        }
    }
}
