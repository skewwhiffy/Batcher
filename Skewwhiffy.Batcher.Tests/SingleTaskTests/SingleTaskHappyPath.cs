using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Skewwhiffy.Batcher.Extensions;
using Skewwhiffy.Batcher.Fluent;
using Skewwhiffy.Batcher.Tests.TestHelpers;

namespace Skewwhiffy.Batcher.Tests.SingleTaskTests
{
    [TestFixture(SynchronicityTestCase.Synchronous, ParallelMultiplicity.SingleThreaded)]
    [TestFixture(SynchronicityTestCase.Asynchronous, ParallelMultiplicity.SingleThreaded)]
    [TestFixture(SynchronicityTestCase.Synchronous, ParallelMultiplicity.MultiThreaded)]
    [TestFixture(SynchronicityTestCase.Asynchronous, ParallelMultiplicity.MultiThreaded)]
    public class SingleTaskHappyPath
    {
        private readonly SynchronicityTestCase _synchronicity;
        private readonly ParallelMultiplicity _multiplicity;
        private SingleTaskBatcherTestSetup _singleTaskBatcherTestSetup;
        private readonly ConcurrentBag<BatchExceptionEventArguments<int>> _exceptionEvents;
        private IBatcher<int> _batcher;

        public SingleTaskHappyPath(SynchronicityTestCase synchronicity, ParallelMultiplicity multiplicity)
        {
            _synchronicity = synchronicity;
            _multiplicity = multiplicity;
            _exceptionEvents = new ConcurrentBag<BatchExceptionEventArguments<int>>();
        }

        [OneTimeSetUp]
        public async Task BeforeAll()
        {
            _singleTaskBatcherTestSetup = new SingleTaskBatcherTestSetup();
            _batcher = _singleTaskBatcherTestSetup.GetBatcher(_synchronicity, _multiplicity);
            _batcher.ExceptionEvent += (o, e) => _exceptionEvents.Add(e as BatchExceptionEventArguments<int>);
            _batcher.Process(_singleTaskBatcherTestSetup.StartItems);
            await _batcher.WaitUntilDone();
            await _singleTaskBatcherTestSetup.WaitUntil(
                s => s.ProcessedItems.Count >= s.StartItems.Count,
                s => s.ProcessedItems.GetMessage());
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            _batcher.Dispose();
        }

        [Test]
        public void NoDuplicates()
        {
            Assert.That(_singleTaskBatcherTestSetup.ProcessedItems.PickOutDuplicates(), Is.Empty);
        }

        [Test]
        public void ActionWorks()
        {
            Assert.That(_singleTaskBatcherTestSetup.ProcessedItems.Count,
                Is.EqualTo(_singleTaskBatcherTestSetup.StartItems.Count),
                _singleTaskBatcherTestSetup.ProcessedItems.GetMessage());
            _singleTaskBatcherTestSetup.StartItems.ForEach(
                s => Assert.That(_singleTaskBatcherTestSetup.ProcessedItems.Contains(s)));
        }

        [Test]
        public void NoExceptions()
        {
            Assert.That(_exceptionEvents, Is.Empty, _exceptionEvents.Select(e => e.Exception.Message).Join());
        }
    }
}