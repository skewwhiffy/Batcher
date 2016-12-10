using System;

namespace Skewwhiffy.Batcher.Extensions
{
    public class BatchExceptionEventArguments<T> : BatchExceptionEventArguments
    {
        public BatchExceptionEventArguments()
        {
            CurrentType = typeof(T);
        }

        public new T Current
        {
            get
            {
                return (T)base.Current;
            }
            set
            {
                base.Current = value;
            }
        }
    }

    public class BatchExceptionEventArguments : EventArgs
    {
        public object Current { get; protected set; }
        public Type CurrentType { get; protected set; }
        public Exception Exception { get; set; }
    }
}
