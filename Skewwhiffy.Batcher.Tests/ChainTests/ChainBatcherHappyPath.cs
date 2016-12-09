using NUnit.Framework;
using Skewwhiffy.Batcher.Tests.TestHelpers;
using System.Threading.Tasks;

namespace Skewwhiffy.Batcher.Tests.ChainTests
{
    [TestFixture(SynchronicityTestCase.Synchronous)]
    [TestFixture(SynchronicityTestCase.Asynchronous)]
    public class ChainBatcherHappyPath
    {
        private ChainBatchAction _batchAction;
        private readonly SynchronicityTestCase _synchronicity;

        public ChainBatcherHappyPath(SynchronicityTestCase synchronicity)
        {
            _synchronicity = synchronicity;
        }

        [OneTimeSetUp]
        public async Task BeforeAll()
        {
            _batchAction = new ChainBatchAction();
            _batchAction.InitializeBatcherStartingWith(_synchronicity);
            _batchAction.StartBatcher();
            await _batchAction.WaitUntilAllProcessed();
        }

        [Test]
        public void ActionWorks()
        {
            Assert.That(_batchAction.ProcessedItemsCount, Is.EqualTo(_batchAction.StartItems.Count));
            var squared = _batchAction.SquaredItems;
            var convertedToString = _batchAction.ConvertedToString;
            var results = _batchAction.Results;
            _batchAction.StartItems.ForEach(s =>
            {
                Assert.That(squared.Contains(s));
                var itemSquared = s * s;
                Assert.That(convertedToString.Contains(itemSquared));
                var squaredString = itemSquared.ToString();
                Assert.That(results.Contains(squaredString));
            });
        }
    }
}
