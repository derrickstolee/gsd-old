using GSD.Common;
using GSD.Common.Git;
using GSD.UnitTests.Mock.Git;
using System.IO;

namespace GSD.UnitTests.Mock.Common
{
    public class MockGSDEnlistment : GSDEnlistment
    {
        private MockGitProcess gitProcess;

        public MockGSDEnlistment()
            : base(Path.Combine("mock:", "path"), "mock://repoUrl", Path.Combine("mock:", "git"), gvfsHooksRoot: null, authentication: null)
        {
            this.GitObjectsRoot = Path.Combine("mock:", "path", ".git", "objects");
            this.LocalObjectsRoot = this.GitObjectsRoot;
            this.GitPackRoot = Path.Combine("mock:", "path", ".git", "objects", "pack");
        }

        public MockGSDEnlistment(string enlistmentRoot, string repoUrl, string gitBinPath, string gvfsHooksRoot, MockGitProcess gitProcess)
            : base(enlistmentRoot, repoUrl, gitBinPath, gvfsHooksRoot, authentication: null)
        {
            this.gitProcess = gitProcess;
        }

        public MockGSDEnlistment(MockGitProcess gitProcess)
            : this()
        {
            this.gitProcess = gitProcess;
        }

        public override string GitObjectsRoot { get; protected set; }

        public override string LocalObjectsRoot { get; protected set; }

        public override string GitPackRoot { get; protected set; }

        public override GitProcess CreateGitProcess()
        {
            return this.gitProcess ?? new MockGitProcess();
        }
    }
}
