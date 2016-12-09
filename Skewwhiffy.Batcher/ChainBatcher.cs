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

        public ChainBatcher(SingleThreadBatcher<T> firstBatcher, List<Func<IEnumerable<Exception>>> getExceptions, List<SingleThreadBatcher> batchers)
        {
            _firstBatcher = firstBatcher;
            _getExceptions = getExceptions;
            batchers.ForEach(b => b.ExceptionEvent += (o, e) => ExceptionEvent(o, e));
        }

        public event ExceptionEventHandler ExceptionEvent;

        public delegate void ExceptionEventHandler(object sender, BatchExceptionEventArguments args);

        public List<Exception> Exceptions => _getExceptions.SelectMany(ge => ge()).ToList();

        public void Process(IEnumerable<T> toProcess) => _firstBatcher.Process(toProcess);
    }
}
