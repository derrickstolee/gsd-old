using System;

namespace GSD.Common.Tracing
{
    public interface IQueuedPipeStringWriterEventSink
    {
        void OnStateChanged(QueuedPipeStringWriter writer, QueuedPipeStringWriterState state, Exception exception);
    }
}
