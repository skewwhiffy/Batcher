using System;
using System.Threading;
using System.Threading.Tasks;
using Skewwhiffy.Batcher.Impl;

namespace Skewwhiffy.Batcher.Fluent
{
    public static class TakeAnItem<TIn>
    {
        public static IBatchChainBuilder<TIn, TOut> Then<TOut>(Func<TIn, TOut> func) => new ChainBuilder<TIn, TOut>(func);
        public static IBatchChainBuilder<TIn, TOut> Then<TOut>(Func<TIn, Task<TOut>> func) => new ChainBuilder<TIn, TOut>(func);
        public static IBatchChainBuilder<TIn, TOut> Then<TOut>(Func<TIn, CancellationToken, Task<TOut>> func) => new ChainBuilder<TIn, TOut>(func);
        public static IBatcher<TIn> And(Action<TIn> action) => new SingleTaskBatcher<TIn>(action);
        public static IBatcher<TIn> And(Func<TIn, Task> action) => new SingleTaskBatcher<TIn>(action);
        public static IBatcher<TIn> And(Func<TIn, CancellationToken, Task> action) => new SingleTaskBatcher<TIn>(action);
    }
}
