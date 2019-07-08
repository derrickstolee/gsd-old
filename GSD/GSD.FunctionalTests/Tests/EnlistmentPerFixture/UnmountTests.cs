using GSD.FunctionalTests.FileSystemRunners;
using GSD.FunctionalTests.Tools;
using GSD.Tests.Should;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GSD.FunctionalTests.Tests.EnlistmentPerFixture
{
    [TestFixture]
    [Category(Categories.ExtraCoverage)]
    public class UnmountTests : TestsWithEnlistmentPerFixture
    {
        private FileSystemRunner fileSystem;

        public UnmountTests()
        {
            this.fileSystem = new SystemIORunner();
        }

        [SetUp]
        public void SetupTest()
        {
            GSDProcess gvfsProcess = new GSDProcess(
                GSDTestConfig.PathToGSD,
                this.Enlistment.EnlistmentRoot,
                Path.Combine(this.Enlistment.EnlistmentRoot, GSDTestConfig.DotGSDRoot));

            if (!gvfsProcess.IsEnlistmentMounted())
            {
                gvfsProcess.Mount();
            }
        }

        [TestCase]
        public void UnmountWaitsForLock()
        {
            ManualResetEventSlim lockHolder = GitHelpers.AcquireGSDLock(this.Enlistment, out _);

            using (Process unmountingProcess = this.StartUnmount())
            {
                unmountingProcess.WaitForExit(3000).ShouldEqual(false, "Unmount completed while lock was acquired.");

                // Release the lock.
                lockHolder.Set();

                unmountingProcess.WaitForExit(10000).ShouldEqual(true, "Unmount didn't complete as expected.");
            }
        }

        [TestCase]
        public void UnmountSkipLock()
        {
            ManualResetEventSlim lockHolder = GitHelpers.AcquireGSDLock(this.Enlistment, out _, Timeout.Infinite, true);

            using (Process unmountingProcess = this.StartUnmount("--skip-wait-for-lock"))
            {
                unmountingProcess.WaitForExit(10000).ShouldEqual(true, "Unmount didn't complete as expected.");
            }

            // Signal process holding lock to terminate and release lock.
            lockHolder.Set();
        }

        private Process StartUnmount(string extraParams = "")
        {
            string enlistmentRoot = this.Enlistment.EnlistmentRoot;

            // TODO: 865304 Use app.config instead of --internal* arguments
            ProcessStartInfo processInfo = new ProcessStartInfo(GSDTestConfig.PathToGSD);
            processInfo.Arguments = "unmount " + extraParams + " " + TestConstants.InternalUseOnlyFlag + " " + GSDHelpers.GetInternalParameter();
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.WorkingDirectory = enlistmentRoot;
            processInfo.UseShellExecute = false;

            Process executingProcess = new Process();
            executingProcess.StartInfo = processInfo;
            executingProcess.Start();

            return executingProcess;
        }
    }
}
