using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Skewwhiffy.Batcher.Extensions;

namespace Skewwhiffy.Batcher.Queue
{
    public class ConcurrentMultiQueue<T>
    {
        private readonly List<ConcurrentQueue<T>> _queues;
        private volatile int _roundRobin;

        public ConcurrentMultiQueue(int queues)
        {
            _queues = 1.To(queues).Select(i => new ConcurrentQueue<T>()).ToList();
        }

        public void Enqueue(T value)
        {
            _queues[_roundRobin].Enqueue(value);
            _roundRobin++;
            if (_roundRobin >= _queues.Count)
            {
                _roundRobin = 0;
            }
        }

        public int Count => _queues.Sum(q => q.Count);

        public List<ConcurrentQueue<T>> Queues => _queues;

        public ConcurrentQueue<T> this[int index] => Queues[index];
    }
}
