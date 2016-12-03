using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skewwhiffy.Batcher
{
    public class SingleThreadBatcher<T>
    {
        private readonly ConcurrentQueue<T> _toProcess;
        private readonly Action<T> _actionSync;
        private readonly Func<T, Task> _actionAsync;
        private readonly List<Exception> _exceptions;

        private SingleThreadBatcher()
        {
            _toProcess = new ConcurrentQueue<T>();
            _exceptions = new List<Exception>();
            Task.Run(() => Process());
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
            _toProcess.Enqueue(toProcess);
        }

        public List<Exception> Exceptions => _exceptions.ToList();

        public event ExceptionEventHandler ExceptionEvent;

        public delegate void ExceptionEventHandler(object sender, BatchExceptionEventArguments<T> args);

        private void OnException(BatchExceptionEventArguments<T> e)
        {
            ExceptionEvent?.Invoke(this, e);
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
                    lock (_exceptions)
                    {
                        OnException(new BatchExceptionEventArguments<T>
                        {
                            Current = current,
                            Exception = ex
                        });
                        _exceptions.Add(ex);
                    }
                }
            }
        }
    }
}
