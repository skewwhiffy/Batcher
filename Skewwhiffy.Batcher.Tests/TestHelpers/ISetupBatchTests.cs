using System.Collections.Generic;
using System.Threading.Tasks;
using Skewwhiffy.Batcher.Fluent;

namespace Skewwhiffy.Batcher.Tests.TestHelpers
{
    public interface ISetupBatchTests
    {
        IBatcher<int> GetBatcher(SynchronicityTestCase synchronicity);
        List<int> StartItems { get; }
        List<int> ProcessedItems { get; }
    }
}
