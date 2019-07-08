using GSD.Common.Git;
using GSD.Common.Tracing;
using System;
using System.IO;

namespace GSD.UnitTests.Mock.Git
{
    public class MockLibGit2Repo : LibGit2Repo
    {
        public MockLibGit2Repo(ITracer tracer)
            : base()
        {
        }

        public override bool CommitAndRootTreeExists(string commitish)
        {
            return false;
        }

        public override bool ObjectExists(string sha)
        {
            return false;
        }

        public override bool TryCopyBlob(string sha, Action<Stream, long> writeAction)
        {
            throw new NotSupportedException();
        }
    }
}
