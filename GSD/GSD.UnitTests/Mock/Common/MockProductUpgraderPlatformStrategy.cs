using GSD.Common;
using GSD.Common.FileSystem;
using GSD.Common.Tracing;

namespace GSD.UnitTests.Mock.Common
{
    public class MockProductUpgraderPlatformStrategy : ProductUpgraderPlatformStrategy
    {
        public MockProductUpgraderPlatformStrategy(PhysicalFileSystem fileSystem, ITracer tracer)
        : base(fileSystem, tracer)
        {
        }

        public override bool TryPrepareLogDirectory(out string error)
        {
            error = null;
            return true;
        }

        public override bool TryPrepareApplicationDirectory(out string error)
        {
            error = null;
            return true;
        }

        public override bool TryPrepareDownloadDirectory(out string error)
        {
            error = null;
            return true;
        }
    }
}
