using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Skewwhiffy.Batcher.Extensions;
using Skewwhiffy.Batcher.Fluent;

namespace Skewwhiffy.Batcher.Impl
{
    internal abstract class SingleTaskBatcher : IDisposable
    {
        protected readonly CancellationTokenSource TokenSource;
        protected readonly List<Exception> ExceptionsInternal;

        protected SingleTaskBatcher()
        {
            ExceptionsInternal = new List<Exception>();
            TokenSource = new CancellationTokenSource();
        }

        public abstract bool IsDone { get; }

        public List<Exception> Exceptions
        {
            get
            {
                lock (ExceptionsInternal)
                {
                    return ExceptionsInternal.ToList();
                }
            }
        }

        public event Event.ExceptionEventHandler ExceptionEvent;

        protected void OnException(BatchExceptionEventArguments e)
        {
            ExceptionEvent?.Invoke(this, e);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                TokenSource.Cancel();
            }
        }
    }

    internal class SingleTaskBatcher<T> : SingleTaskBatcher, IBatcher<T>
    {
        private readonly ConcurrentQueue<T> _toProcess;
        private readonly Action<T> _actionSync;
        private readonly Func<T, CancellationToken, Task> _actionAsync;
        private Task _processor;
        private volatile bool _waiting;

        private SingleTaskBatcher()
        {
            _toProcess = new ConcurrentQueue<T>();
        }

        public SingleTaskBatcher(Action<T> action) : this()
        {
            _actionSync = action;
        }

        public SingleTaskBatcher(Func<T, Task> action) : this()
        {
            _actionAsync = (t, ct) => action(t);
        }

        public SingleTaskBatcher(Func<T, CancellationToken, Task> action) : this()
        {
            _actionAsync = action;
        }

        public override bool IsDone => (_waiting && _toProcess.Count == 0) || _processor == null || _processor.IsCompleted;

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
                _processor = Task.Run(() => Process(TokenSource.Token), TokenSource.Token);
            }
        }

        private async Task Process(CancellationToken token)
        {
            T current = default(T);
            while (true)
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    _waiting = !_toProcess.TryDequeue(out current);
                    if (_waiting)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), token);
                        continue;
                    }
                    _actionSync?.Invoke(current);
                    if (_actionAsync != null)
                    {
                        await _actionAsync(current, token);
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
