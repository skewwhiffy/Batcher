using System.Threading.Tasks;
using NUnit.Framework;
using Skewwhiffy.Batcher.Fluent;
using Skewwhiffy.Batcher.Tests.TestHelpers;

namespace Skewwhiffy.Batcher.Tests.SingleTaskTests
{
    [TestFixture(SynchronicityTestCase.Synchronous)]
    [TestFixture(SynchronicityTestCase.Asynchronous)]
    public class SingleTaskHappyPath
    {
        private readonly SynchronicityTestCase _synchronicity;
        private SingleTaskBatcherTestSetup _singleTaskBatcherTestSetup;
        private IBatcher<int> _batcher;

        public SingleTaskHappyPath(SynchronicityTestCase synchronicity)
        {
            _synchronicity = synchronicity;
        }

        [OneTimeSetUp]
        public async Task BeforeAll()
        {
            _singleTaskBatcherTestSetup = new SingleTaskBatcherTestSetup();
            _batcher = _singleTaskBatcherTestSetup.GetBatcher(_synchronicity);
            _batcher.Process(_singleTaskBatcherTestSetup.StartItems);
            await _batcher.WaitUntilDone();
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            _batcher.Dispose();
        }

        [Test]
        public void ActionWorks()
        {
            Assert.That(_singleTaskBatcherTestSetup.StartItems.Count, Is.EqualTo(_singleTaskBatcherTestSetup.ProcessedItems.Count));
            _singleTaskBatcherTestSetup.StartItems.ForEach(s => Assert.That(_singleTaskBatcherTestSetup.ProcessedItems.Contains(s)));
        }
    }
}