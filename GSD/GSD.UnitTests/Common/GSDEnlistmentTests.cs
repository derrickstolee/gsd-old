using GSD.Common;
using GSD.Common.Git;
using GSD.Tests.Should;
using GSD.UnitTests.Mock.Common;
using GSD.UnitTests.Mock.Git;
using NUnit.Framework;

namespace GSD.UnitTests.Common
{
    [TestFixture]
    public class GSDEnlistmentTests
    {
        private const string MountId = "85576f54f9ab4388bcdc19b4f6c17696";
        private const string EnlistmentId = "520dcf634ce34065a06abaa4010a256f";

        [TestCase]
        public void CanGetMountId()
        {
            TestGSDEnlistment enlistment = new TestGSDEnlistment();
            enlistment.GetMountId().ShouldEqual(MountId);
        }

        [TestCase]
        public void CanGetEnlistmentId()
        {
            TestGSDEnlistment enlistment = new TestGSDEnlistment();
            enlistment.GetEnlistmentId().ShouldEqual(EnlistmentId);
        }

        private class TestGSDEnlistment : GSDEnlistment
        {
            private MockGitProcess gitProcess;

            public TestGSDEnlistment()
                : base("mock:\\path", "mock://repoUrl", "mock:\\git", gvfsHooksRoot: null, authentication: null)
            {
                this.gitProcess = new MockGitProcess();
                this.gitProcess.SetExpectedCommandResult(
                    "config --local gvfs.mount-id",
                    () => new GitProcess.Result(MountId, string.Empty, GitProcess.Result.SuccessCode));
                this.gitProcess.SetExpectedCommandResult(
                    "config --local gvfs.enlistment-id",
                    () => new GitProcess.Result(EnlistmentId, string.Empty, GitProcess.Result.SuccessCode));
            }

            public override GitProcess CreateGitProcess()
            {
                return this.gitProcess;
            }
        }
    }
}
