using GSD.Common.FileSystem;
using GSD.Common.Git;
using GSD.Common.Http;
using GSD.Common.Tracing;

namespace GSD.Common.Prefetch.Git
{
    public class PrefetchGitObjects : GitObjects
    {
        public PrefetchGitObjects(ITracer tracer, Enlistment enlistment, GitObjectsHttpRequestor objectRequestor, PhysicalFileSystem fileSystem = null) : base(tracer, enlistment, objectRequestor, fileSystem)
        {
        }
    }
}
