using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skewwhiffy.Batcher
{
    public class ChainBatcher<T>
    {
        private List<Func<IEnumerable<Exception>>> _getExceptions;
        private SingleThreadBatcher<T> _firstBatcher;
        private SingleThreadBatcher<int> _secondBatcher;
        private SingleThreadBatcher<string> _finalBatcher;

        public ChainBatcher(Func<T, int> first, Func<int, Task<string>> second, Action<string> final)
        {
            _finalBatcher = new SingleThreadBatcher<string>(final);
            _secondBatcher = new SingleThreadBatcher<int>(async i =>
            {
                var result = await second(i);
                _finalBatcher.Process(result);
            });
            _firstBatcher = new SingleThreadBatcher<T>(i =>
            {
                var result = first(i);
                _secondBatcher.Process(result);
            });

            _firstBatcher.ExceptionEvent += (o, e) => ExceptionEvent(o, e);
            _secondBatcher.ExceptionEvent += (o, e) => ExceptionEvent(o, e);
            _finalBatcher.ExceptionEvent += (o, e) => ExceptionEvent(o, e);

            _getExceptions = new List<Func<IEnumerable<Exception>>>
            {
                () => _firstBatcher.Exceptions,
                () => _secondBatcher.Exceptions,
                () => _finalBatcher.Exceptions
            };
        }
        public event ExceptionEventHandler ExceptionEvent;

        public delegate void ExceptionEventHandler(object sender, BatchExceptionEventArguments args);

        public List<Exception> Exceptions
        {
            get
            {
                var exceptions = _firstBatcher.Exceptions.ToList();
                exceptions.AddRange(_secondBatcher.Exceptions);
                exceptions.AddRange(_finalBatcher.Exceptions);
                return exceptions;
            }
        }

        public void Process(IEnumerable<T> toProcess)
        {
            _firstBatcher.Process(toProcess);
        }
    }
}
