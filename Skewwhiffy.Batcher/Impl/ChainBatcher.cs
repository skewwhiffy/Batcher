using System;
using System.Collections.Generic;
using System.Linq;
using Skewwhiffy.Batcher.Fluent;

namespace Skewwhiffy.Batcher.Impl
{
    internal class ChainBatcher<T> : IBatcher<T>
    {
        private readonly List<Func<IEnumerable<Exception>>> _getExceptions;
        private readonly SingleTaskBatcher<T> _firstBatcher;
        private readonly List<SingleTaskBatcher> _batchers;

        internal ChainBatcher(
            SingleTaskBatcher<T> firstBatcher,
            List<Func<IEnumerable<Exception>>> getExceptions,
            List<SingleTaskBatcher> batchers)
        {
            _firstBatcher = firstBatcher;
            _getExceptions = getExceptions;
            _batchers = batchers;
            batchers.ForEach(b => b.ExceptionEvent += (o, e) => ExceptionEvent?.Invoke(o, e));
        }

        public IBatcher<T> WithThreads(int threads)
        {
            throw new NotImplementedException();
        }

        public bool IsDone => _batchers.All(b => b.IsDone);

        public event Event.ExceptionEventHandler ExceptionEvent;

        public List<Exception> Exceptions => _getExceptions.SelectMany(ge => ge()).ToList();

        public void Process(IEnumerable<T> toProcess) => _firstBatcher.Process(toProcess);

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                _batchers.ForEach(b => b.Dispose());
            }
        }
    }
}
