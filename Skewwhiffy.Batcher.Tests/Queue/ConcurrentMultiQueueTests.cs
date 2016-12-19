using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Skewwhiffy.Batcher.Extensions;
using Skewwhiffy.Batcher.Queue;

namespace Skewwhiffy.Batcher.Tests.Queue
{
    public class ConcurrentMultiQueueTests
    {
        private const int NumberOfQueues = 10;
        private List<int> _source;
        private List<List<int>> _results;
        private ConcurrentMultiQueue<int> _multiQueue;

        [OneTimeSetUp]
        public void BeforeAll()
        {
            _source = 0.To(999).ToList();
            _results = Enumerable.Range(0, NumberOfQueues).Select(i => new List<int>()).ToList();
            _multiQueue = new ConcurrentMultiQueue<int>(NumberOfQueues);

            _source.ForEach(s => _multiQueue.Enqueue(s));

            _results = _multiQueue.Queues.Select(q => q.ToList()).ToList();
        }

        [Test]
        public void DataIsDistributedEvenly()
        {
            Assert.That(_results.Max(r => r.Count) - _results.Min(r => r.Count), Is.LessThan(1));
        }

        [Test]
        public void DataIsDistributed()
        {
            Assert.That(_results.Sum(q => q.Count), Is.EqualTo(_source.Count));
        }

        [Test]
        public void DataIsCounted()
        {
            Assert.That(_multiQueue.Count, Is.EqualTo(_source.Count));
        }
    }
}
