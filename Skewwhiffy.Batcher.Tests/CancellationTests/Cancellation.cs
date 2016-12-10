using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Skewwhiffy.Batcher.Fluent;
using Skewwhiffy.Batcher.Tests.TestHelpers;

namespace Skewwhiffy.Batcher.Tests.CancellationTests
{
    [TestFixture(SynchronicityTestCase.Synchronous, TaskMultiplicityTestCase.Single)]
    [TestFixture(SynchronicityTestCase.Asynchronous, TaskMultiplicityTestCase.Single)]
    [TestFixture(SynchronicityTestCase.Synchronous, TaskMultiplicityTestCase.Multiple)]
    [TestFixture(SynchronicityTestCase.Asynchronous, TaskMultiplicityTestCase.Multiple)]
    public class Cancellation
    {
        private readonly SynchronicityTestCase _synchronicity;
        private readonly TaskMultiplicityTestCase _taskMultiplicity;
        private ISetupBatchTests _setup;
        private IBatcher<int> _batcher;

        private IBatcher<int> Batcher
        {
            get
            {
                if (_batcher != null)
                {
                    return _batcher;
                }
                switch (_taskMultiplicity)
                {
                    case TaskMultiplicityTestCase.Single:
                        _setup = new SingleTaskBatcherTestSetup
                        {
                            PauseBetweenProcessing = TimeSpan.FromSeconds(0.1)
                        };
                        return _batcher = _setup.GetBatcher(_synchronicity);
                    case TaskMultiplicityTestCase.Multiple:
                        _setup = new ChainBatcherTestSetup
                        {
                            PauseBetweenProcessing = TimeSpan.FromSeconds(0.1)
                        };
                        return _batcher = _setup.GetBatcher(_synchronicity);
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public Cancellation(SynchronicityTestCase synchronicity, TaskMultiplicityTestCase taskMultiplicity)
        {
            _synchronicity = synchronicity;
            _taskMultiplicity = taskMultiplicity;
        }

        [OneTimeSetUp]
        public void BeforeAll()
        {
            Batcher.Process(_setup.StartItems);
        }

        [Test]
        public async Task CancellationWorks()
        {
            Batcher.Dispose();

            await Batcher.WaitUntilDone();

            Assert.That(_setup.StartItems.Count, Is.GreaterThan(_setup.ProcessedItems.Count));
        }
    }
}
