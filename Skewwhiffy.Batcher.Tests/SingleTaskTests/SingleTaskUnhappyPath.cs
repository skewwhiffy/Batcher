using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Skewwhiffy.Batcher.Extensions;
using Skewwhiffy.Batcher.Fluent;
using Skewwhiffy.Batcher.Tests.TestHelpers;

namespace Skewwhiffy.Batcher.Tests.SingleTaskTests
{
    [TestFixture(SynchronicityTestCase.Synchronous)]
    [TestFixture(SynchronicityTestCase.Asynchronous)]
    public class SingleTaskUnhappyPath
    {
        private readonly SynchronicityTestCase _synchronicity;
        private SingleTaskBatcherTestSetup _singleTaskBatcherTestSetup;
        private Func<int, bool> _throwWhen;
        private readonly ConcurrentBag<BatchExceptionEventArguments<int>> _exceptionEvents;
        private IBatcher<int> _batcher;

        public SingleTaskUnhappyPath(SynchronicityTestCase synchronicity)
        {
            _synchronicity = synchronicity;
            _exceptionEvents = new ConcurrentBag<BatchExceptionEventArguments<int>>();
        }

        [OneTimeSetUp]
        public async Task BeforeEach()
        {
            _singleTaskBatcherTestSetup = new SingleTaskBatcherTestSetup();
            _throwWhen = i => i % 2 == 0;
            _singleTaskBatcherTestSetup.ThrowWhen = _throwWhen;
            _batcher = _singleTaskBatcherTestSetup.GetBatcher(_synchronicity);
            _batcher.ExceptionEvent += (o, e) => _exceptionEvents.Add(e as BatchExceptionEventArguments<int>);
            _batcher.Process(_singleTaskBatcherTestSetup.StartItems);
            await _batcher.WaitUntilDone();
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            _batcher.Dispose();
        }

        [Test]
        public void AllItemsAreProcessed()
        {
            _singleTaskBatcherTestSetup.StartItems.ForEach(s => Assert.That(_singleTaskBatcherTestSetup.ProcessedItems.Contains(s)));
        }

        [Test]
        public void ExceptionsCaptured()
        {
            var expected = _singleTaskBatcherTestSetup.ProcessedItems.Count(i => !_throwWhen(i));
            Assert.That(_batcher.Exceptions.Count, Is.EqualTo(expected));
        }

        [Test]
        public void ExceptionEventsRaised()
        {
            var expected = _singleTaskBatcherTestSetup.ProcessedItems.Count(i => !_throwWhen(i));
            Assert.That(_exceptionEvents.Count, Is.EqualTo(expected));
        }
    }
}
