using System;
using System.Threading;
using System.Threading.Tasks;

namespace Skewwhiffy.Batcher.Fluent
{
    public interface IBatchChainBuilder<in TIn, out TOut>
    {
        IBatchChainBuilder<TIn, TNewOut> Then<TNewOut>(Func<TOut, TNewOut> func);
        IBatchChainBuilder<TIn, TNewOut> Then<TNewOut>(Func<TOut, Task<TNewOut>> func);
        IBatchChainBuilder<TIn, TNewOut> Then<TNewOut>(Func<TOut, CancellationToken, Task<TNewOut>> func);
        IBatcher<TIn> AndFinally(Action<TOut> action);
        IBatcher<TIn> AndFinally(Func<TOut, Task> action);
        IBatcher<TIn> AndFinally(Func<TOut, CancellationToken, Task> action);
    }
}
