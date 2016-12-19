using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Skewwhiffy.Batcher.Tests.TestHelpers;
using System.Threading.Tasks;
using Skewwhiffy.Batcher.Fluent;
using Skewwhiffy.Batcher.Extensions;

namespace Skewwhiffy.Batcher.Tests.ChainTests
{
    [TestFixture(SynchronicityTestCase.Synchronous, ParallelMultiplicity.SingleThreaded)]
    [TestFixture(SynchronicityTestCase.Asynchronous, ParallelMultiplicity.SingleThreaded)]
    [TestFixture(SynchronicityTestCase.Synchronous, ParallelMultiplicity.MultiThreaded)]
    [TestFixture(SynchronicityTestCase.Asynchronous, ParallelMultiplicity.MultiThreaded)]
    public class ChainBatcherHappyPath
    {
        private ChainBatcherTestSetup _setup;
        private readonly SynchronicityTestCase _synchronicity;
        private readonly ParallelMultiplicity _multiplicity;
        private IBatcher<int> _batcher;

        public ChainBatcherHappyPath(SynchronicityTestCase synchronicity, ParallelMultiplicity multiplicity)
        {
            _synchronicity = synchronicity;
            _multiplicity = multiplicity;
        }

        [OneTimeSetUp]
        public async Task BeforeAll()
        {
            _setup = new ChainBatcherTestSetup();
            _batcher = _setup.GetBatcher(_synchronicity, _multiplicity);
            _batcher.Process(_setup.StartItems);
            await _batcher.WaitUntilDone();
            await _setup.WaitUntil(s => s.ProcessedItems.Count >= s.StartItems.Count,
                    s => s.ProcessedItems.GetMessage());
            await
                _setup.WaitUntil(s => s.SquaredItems.Count >= s.StartItems.Count, s => s.SquaredItems.GetMessage());
            await _setup.WaitUntil(s => s.ConvertedToString.Count == s.StartItems.Count,
                s => s.ConvertedToString.GetMessage());
            await _setup.WaitUntil(s => s.Results.Count >= s.StartItems.Count, s => s.Results.GetMessage());
        }


        [OneTimeTearDown]
        public void AfterAll()
        {
            _batcher.Dispose();
        }

        [Test]
        public void NoDuplicates()
        {
            Assert.That(_setup.ProcessedItems.PickOutDuplicates(), Is.Empty);
            Assert.That(_setup.SquaredItems.PickOutDuplicates(), Is.Empty);
            Assert.That(_setup.ConvertedToString.PickOutDuplicates(), Is.Empty);
            Assert.That(_setup.Results.PickOutDuplicates(), Is.Empty);
        }

        [Test]
        public void CountsCorrect()
        {
            Assert.That(_setup.ProcessedItems.Count, Is.EqualTo(_setup.StartItems.Count), _setup.ProcessedItems.GetMessage());
            Assert.That(_setup.SquaredItems.Count, Is.EqualTo(_setup.StartItems.Count), _setup.SquaredItems.GetMessage());
            Assert.That(_setup.ConvertedToString.Count, Is.EqualTo(_setup.ConvertedToString.Count), _setup.SquaredItems.GetMessage());
            Assert.That(_setup.Results.Count, Is.EqualTo(_setup.Results.Count), _setup.SquaredItems.GetMessage());
        }

        [Test]
        public void ProcessingCorrect()
        {
            var squared = _setup.SquaredItems;
            var convertedToString = _setup.ConvertedToString;
            var results = _setup.Results;
            _setup.StartItems.ForEach(s =>
            {
                Assert.That(squared.Contains(s), $"{s} is missing: {squared.GetMessage()}");
                var itemSquared = s*s;
                Assert.That(convertedToString.Contains(itemSquared),
                    $"{itemSquared} is missing: {convertedToString.GetMessage()}");
                var squaredString = itemSquared.ToString();
                Assert.That(results.Contains(squaredString), $"{squaredString} is missing: {results.GetMessage()}");
            });
        }
    }
}
