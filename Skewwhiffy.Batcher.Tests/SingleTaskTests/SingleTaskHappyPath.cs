using System.Threading.Tasks;
using NUnit.Framework;
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
        private IBatcher<int> _batcher;

        public SingleTaskHappyPath(SynchronicityTestCase synchronicity, ParallelMultiplicity multiplicity)
        {
            _synchronicity = synchronicity;
            _multiplicity = multiplicity;
        }

        [OneTimeSetUp]
        public async Task BeforeAll()
        {
            _singleTaskBatcherTestSetup = new SingleTaskBatcherTestSetup();
            _batcher = _singleTaskBatcherTestSetup.GetBatcher(_synchronicity, _multiplicity);
            _batcher.Process(_singleTaskBatcherTestSetup.StartItems);
            await _batcher.WaitUntilDone();
            await BatcherExtensions.WaitUntilConstant(() => _singleTaskBatcherTestSetup.ProcessedItems.Count);
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            _batcher.Dispose();
        }

        [Test]
        public void ActionWorks()
        {
            Assert.That(_singleTaskBatcherTestSetup.ProcessedItems.Count, Is.EqualTo(_singleTaskBatcherTestSetup.StartItems.Count));
            _singleTaskBatcherTestSetup.StartItems.ForEach(s => Assert.That(_singleTaskBatcherTestSetup.ProcessedItems.Contains(s)));
        }
    }
}