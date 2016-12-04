using NUnit.Framework;
using Skewwhiffy.Batcher.Tests.TestHelpers;
using System.Threading.Tasks;

namespace Skewwhiffy.Batcher.Tests.SingleThreadTests
{
    [TestFixture(SynchronicityTestCase.Synchronous)]
    [TestFixture(SynchronicityTestCase.Asynchronous)]
    public class SingleThreadHappyPath
    {
        private SynchronicityTestCase _synchronicity;
        private BatchAction _batchAction;

        public SingleThreadHappyPath(SynchronicityTestCase synchronicity)
        {
            _synchronicity = synchronicity;
        }

        [OneTimeSetUp]
        public async Task BeforeAll()
        {
            _batchAction = new BatchAction();
            _batchAction.InitializeBatcher(_synchronicity);
            _batchAction.StartBatcher();
            await _batchAction.WaitUntilAllProcessed();
        }

        [Test]
        public void ActionWorks()
        {
            Assert.That(_batchAction.StartItems.Count, Is.EqualTo(_batchAction.ProcessedItems.Count));
            _batchAction.StartItems.ForEach(s => Assert.That(_batchAction.ProcessedItems.Contains(s)));
        }
    }
}