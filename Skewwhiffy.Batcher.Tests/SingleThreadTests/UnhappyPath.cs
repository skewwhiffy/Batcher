﻿using NUnit.Framework;
using Skewwhiffy.Batcher.Tests.TestHelpers;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Skewwhiffy.Batcher.Tests.SingleThreadTests
{
    [TestFixture(SynchronicityTestCase.Synchronous)]
    [TestFixture(SynchronicityTestCase.Asynchronous)]
    public class UnhappyPath
    {
        private SynchronicityTestCase _synchronicity;
        private BatchAction _batchAction;
        private Func<int, bool> _throwWhen;
        private ConcurrentBag<BatchExceptionEventArguments<int>> _exceptionEvents;

        public UnhappyPath(SynchronicityTestCase synchronicity)
        {
            _synchronicity = synchronicity;
            _exceptionEvents = new ConcurrentBag<BatchExceptionEventArguments<int>>();
        }

        [OneTimeSetUp]
        public async Task BeforeEach()
        {
            _batchAction = new BatchAction();
            _throwWhen = i => i % 2 == 0;
            _batchAction.ThrowWhen(_throwWhen);
            _batchAction.InitializeBatcher(_synchronicity);
            _batchAction.Batcher.ExceptionEvent += (o, e) => _exceptionEvents.Add(e);
            _batchAction.StartBatcher();
            await _batchAction.WaitUntilAllProcessed();
        }

        [Test]
        public void ActionWithThrowsWorks()
        {
            _batchAction.StartItems.ForEach(s => Assert.That(_batchAction.ProcessedItems.Contains(s)));
        }

        [Test]
        public void ExceptionsCaught()
        {
            var expected = _batchAction.ProcessedItems.Count(i => !_throwWhen(i));
            Assert.That(_batchAction.Batcher.Exceptions.Count, Is.EqualTo(expected));
        }

        [Test]
        public void ExceptionEventsThrown()
        {
            var expected = _batchAction.ProcessedItems.Count(i => !_throwWhen(i));
            Assert.That(_exceptionEvents.Count, Is.EqualTo(expected));
        }
    }
}
