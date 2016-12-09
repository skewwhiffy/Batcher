using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skewwhiffy.Batcher
{
    public abstract class SingleThreadBatcher
    {
        protected readonly List<Exception> ExceptionsInternal;

        protected SingleThreadBatcher()
        {
            ExceptionsInternal = new List<Exception>();
        }

        public List<Exception> Exceptions => ExceptionsInternal.ToList();

        public event ExceptionEventHandler ExceptionEvent;

        public delegate void ExceptionEventHandler(object sender, BatchExceptionEventArguments args);

        protected void OnException(BatchExceptionEventArguments e)
        {
            ExceptionEvent?.Invoke(this, e);
        }
    }

    public class SingleThreadBatcher<T> : SingleThreadBatcher
    {
        private readonly ConcurrentQueue<T> _toProcess;
        private readonly Action<T> _actionSync;
        private readonly Func<T, Task> _actionAsync;
        private Task _processor;

        private SingleThreadBatcher()
        {
            _toProcess = new ConcurrentQueue<T>();
        }

        public SingleThreadBatcher(Action<T> action) : this()
        {
            _actionSync = action;
        }

        public SingleThreadBatcher(Func<T, Task> action) : this()
        {
            _actionAsync = action;
        }

        public void Process(IEnumerable<T> toProcess)
        {
            toProcess.ForEach(Process);
        }

        public void Process(T toProcess)
        {
            Start();
            _toProcess.Enqueue(toProcess);
        }

        private void Start()
        {
            if (_processor == null)
            {
                _processor = Task.Run(() => Process());
            }
        }

        private async Task Process()
        {
            T current = default(T);
            while (true)
            {
                try
                {
                    if (!_toProcess.TryDequeue(out current))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        continue;
                    }
                    _actionSync?.Invoke(current);
                    if (_actionAsync != null)
                    {
                        await _actionAsync(current);
                    }
                    current = default(T);
                }
                catch (Exception ex)
                {
                    lock (ExceptionsInternal)
                    {
                        OnException(new BatchExceptionEventArguments<T>
                        {
                            Current = current,
                            Exception = ex
                        });
                        ExceptionsInternal.Add(ex);
                    }
                }
            }
        }
    }
}
