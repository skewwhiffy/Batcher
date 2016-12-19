using System;
using System.Collections.Generic;
using Skewwhiffy.Batcher.Impl;

namespace Skewwhiffy.Batcher.Fluent
{
    public interface IBatcher<in T> : IDisposable
    {
        event Event.ExceptionEventHandler ExceptionEvent;
        IBatcher<T> WithThreads(int threads);
        void Process(IEnumerable<T> toProcess);
        List<Exception> Exceptions { get; }
        bool IsDone { get; }
    }
}
