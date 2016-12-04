using NUnit.Framework;
using Skewwhiffy.Batcher.Tests.TestHelpers;
using System.Threading.Tasks;

namespace Skewwhiffy.Batcher.Tests.ChainTests
{
    public class ChainBatcherHappyPath
    {
        private ChainBatchAction _batchAction;

        [OneTimeSetUp]
        public async Task BeforeAll()
        {
            _batchAction = new ChainBatchAction();
            _batchAction.InitializeBatcher();
            _batchAction.StartBatcher();
            await _batchAction.WaitUntilAllProcessed();
        }

        [Test]
        public void ActionWorks()
        {
        }
    }
}
