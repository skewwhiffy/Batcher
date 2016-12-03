using System;

namespace Skewwhiffy.Batcher
{
    public class BatchExceptionEventArguments<T> : EventArgs
    {
        public T Current { get; set; }
        public Exception Exception { get; set; }
    }
}
