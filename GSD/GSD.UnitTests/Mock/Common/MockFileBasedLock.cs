using GSD.Common;
using GSD.Common.FileSystem;
using GSD.Common.Tracing;

namespace GSD.UnitTests.Mock.Common
{
    public class MockFileBasedLock : FileBasedLock
    {
        public MockFileBasedLock(
            PhysicalFileSystem fileSystem,
            ITracer tracer,
            string lockPath)
            : base(fileSystem, tracer, lockPath)
        {
        }

        public override bool TryAcquireLock()
        {
            return true;
        }

        public override void Dispose()
        {
        }
    }
}
