using NUnit.Framework;
using Skewwhiffy.Batcher.Tests.TestHelpers;
using System.Threading.Tasks;
using Skewwhiffy.Batcher.Fluent;

namespace Skewwhiffy.Batcher.Tests.ChainTests
{
    [TestFixture(SynchronicityTestCase.Synchronous)]
    [TestFixture(SynchronicityTestCase.Asynchronous)]
    public class ChainBatcherHappyPath
    {
        private ChainBatcherTestSetup _setup;
        private readonly SynchronicityTestCase _synchronicity;
        private IBatcher<int> _batcher;

        public ChainBatcherHappyPath(SynchronicityTestCase synchronicity)
        {
            _synchronicity = synchronicity;
        }

        [OneTimeSetUp]
        public async Task BeforeAll()
        {
            _setup = new ChainBatcherTestSetup();
            _batcher = _setup.GetBatcher(_synchronicity);
            _batcher.Process(_setup.StartItems);
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
            Assert.That(_setup.ProcessedItems.Count, Is.EqualTo(_setup.StartItems.Count));
            var squared = _setup.SquaredItems;
            var convertedToString = _setup.ConvertedToString;
            var results = _setup.Results;
            _setup.StartItems.ForEach(s =>
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
