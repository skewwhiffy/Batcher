﻿using NUnit.Framework;
using Skewwhiffy.Batcher.Tests.TestHelpers;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;
using System;
using Skewwhiffy.Batcher.Extensions;
using Skewwhiffy.Batcher.Fluent;

namespace Skewwhiffy.Batcher.Tests.ChainTests
{
    [TestFixture(SynchronicityTestCase.Synchronous)]
    [TestFixture(SynchronicityTestCase.Asynchronous)]
    public class ChainBatcherUnhappyPath
    {
        private readonly SynchronicityTestCase _synchronicity;
        private ChainBatcherTestSetup _setup;
        private IBatcher<int> _batcher;
        private ConcurrentBag<Tuple<object, BatchExceptionEventArguments>> _batchExceptionEvents;

        public ChainBatcherUnhappyPath(SynchronicityTestCase synchronicity)
        {
            _synchronicity = synchronicity;
        }

        [OneTimeSetUp]
        public async Task BeforeAll()
        {
            _batchExceptionEvents = new ConcurrentBag<Tuple<object, BatchExceptionEventArguments>>();
            _setup = new ChainBatcherTestSetup();
            _setup.ThrowWhenSquaring(ThrowWhenSquaring);
            _setup.ThrowWhenConvertingToString(ThrowWhenConvertingToString);
            _setup.ThrowWhenPuttingInResultsBag(ThrowWhenPuttingInResultsBag);
            _batcher = _setup.GetBatcher(_synchronicity);
            _batcher.ExceptionEvent += (o, e) => _batchExceptionEvents.Add(Tuple.Create(o, e));
            _batcher.Process(_setup.StartItems);
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
            var squared = _setup.SquaredItems;
            var convertedToString = _setup.ConvertedToString;
            var results = _setup.Results;
            _setup.StartItems.ForEach(s =>
            {
                if (ThrowWhenSquaring(s))
                {
                    Assert.That(squared.Contains(s), Is.False);
                    return;
                }

                Assert.That(squared.Contains(s));
                var itemSquared = s * s;
                if (ThrowWhenConvertingToString(itemSquared))
                {
                    Assert.That(convertedToString.Contains(itemSquared), Is.False);
                    return;
                }

                Assert.That(convertedToString.Contains(itemSquared));
                var squaredString = itemSquared.ToString();
                if (ThrowWhenPuttingInResultsBag(squaredString))
                {
                    Assert.That(results.Contains(squaredString), Is.False);
                    return;
                }
                Assert.That(results.Contains(squaredString), $"Expected results to contain {squaredString}");
            });
        }

        [Test]
        public void ExceptionsCaptured()
        {
            var expected = 0;
            var start = _setup.StartItems;
            expected += start.Count(ThrowWhenSquaring);
            var squared = start.FindAll(i => !ThrowWhenSquaring(i)).Select(i => i * i).ToList();
            expected += squared.Count(ThrowWhenConvertingToString);
            var convertedToString = squared.FindAll(i => !ThrowWhenConvertingToString(i)).Select(i => i.ToString()).ToList();
            expected += convertedToString.Count(ThrowWhenPuttingInResultsBag);

            var actual = _batcher.Exceptions;
            Assert.That(actual.Count, Is.EqualTo(expected));
        }

        [Test]
        public void ExceptionEventsRaised()
        {
            var actual = _batchExceptionEvents.GroupBy(e => e.Item1).ToDictionary(g => g.Key, g => g.Select(i => i.Item2).ToList());

            var start = _setup.StartItems;
            Assert.That(actual.Any(kvp => kvp.Value.Count == start.Count(ThrowWhenSquaring)));

            var squared = start.FindAll(i => !ThrowWhenSquaring(i)).Select(i => i * i).ToList();
            Assert.That(actual.Any(kvp => kvp.Value.Count == squared.Count(ThrowWhenConvertingToString)));

            var convertedToString = squared.FindAll(i => !ThrowWhenConvertingToString(i)).Select(i => i.ToString()).ToList();
            Assert.That(actual.Any(kvp => kvp.Value.Count == convertedToString.Count(ThrowWhenPuttingInResultsBag)));
        }

        private bool ThrowWhenSquaring(int i) => i % 2 == 0;

        private bool ThrowWhenConvertingToString(int i) => i > 2500;

        private bool ThrowWhenPuttingInResultsBag(string i) => i.Length % 2 == 0;
    }
}
