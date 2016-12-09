using Skewwhiffy.Batcher.Extensions;

namespace Skewwhiffy.Batcher.Impl
{
    public static class Event
    {
        public delegate void ExceptionEventHandler(object sender, BatchExceptionEventArguments args);
    }
}
