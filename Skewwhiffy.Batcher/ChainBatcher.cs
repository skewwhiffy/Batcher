using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skewwhiffy.Batcher
{
    public class ChainBatcher<T>
    {
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
        }

        public void Process(IEnumerable<T> toProcess)
        {
            _firstBatcher.Process(toProcess);
        }
    }
}
