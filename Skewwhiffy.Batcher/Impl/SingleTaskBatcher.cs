﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Skewwhiffy.Batcher.Extensions;
using Skewwhiffy.Batcher.Fluent;
using Skewwhiffy.Batcher.Queue;

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

        public void WithThreads(int threads) => Threads = threads;

        public int? Threads { get; private set; }

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

        protected void OnException(BatchExceptionEventArguments e) => ExceptionEvent?.Invoke(this, e);

        public void Dispose() => Dispose(true);

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
        private readonly Action<T> _actionSync;
        private readonly Func<T, CancellationToken, Task> _actionAsync;
        private List<Task> _processor;
        private volatile bool _waiting;
        private readonly Lazy<ConcurrentMultiQueue<T>> _toProcess;

        private SingleTaskBatcher()
        {
            _toProcess = new Lazy<ConcurrentMultiQueue<T>>(() => new ConcurrentMultiQueue<T>(Threads ?? 1));
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

        public new SingleTaskBatcher<T> WithThreads(int threads)
        {
            base.WithThreads(threads);
            return this;
        }

        IBatcher<T> IBatcher<T>.WithThreads(int threads)
        {
            return WithThreads(threads);
        }

        private ConcurrentMultiQueue<T> ToProcess => _toProcess.Value;

        public override bool IsDone => (_waiting && ToProcess.Count == 0) || _processor == null || _processor.All(p => p.IsCompleted);

        public void Process(IEnumerable<T> toProcess) => toProcess.ForEach(Process);

        public void Process(T toProcess)
        {
            Start();
            ToProcess.Enqueue(toProcess);
        }

        private void Start()
        {
            if (_processor != null)
            {
                return;
            }
            lock (this)
            {
                if (_processor != null)
                {
                    return;
                }
                _processor = new List<Task>();
            }
            for (var i = 0; i < (Threads ?? 1); i++)
            {
                var queue = ToProcess[i];
                _processor.Add(Task.Run(() => Process(queue, TokenSource.Token), TokenSource.Token));
            }
        }

        private async Task Process(ConcurrentQueue<T> queue, CancellationToken token)
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
                    var waiting = !queue.TryDequeue(out current);
                    if (waiting)
                    {
                        _waiting = true;
                        await Task.Delay(TimeSpan.FromSeconds(1), token);
                        continue;
                    }
                    _waiting = false;
                    if (current.Equals(default(T)))
                    {
                        Console.WriteLine("HERE");
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
