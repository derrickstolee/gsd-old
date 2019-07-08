using GSD.FunctionalTests.Tools;
using GSD.Tests.Should;
using NUnit.Framework;

namespace GSD.FunctionalTests.Tests.MultiEnlistmentTests
{
    [TestFixture]
    [NonParallelizable]
    [Category(Categories.ExtraCoverage)]
    [Category(Categories.MacTODO.NeedsServiceVerb)]
    public class ServiceVerbTests : TestsWithMultiEnlistment
    {
        private static readonly string[] EmptyRepoList = new string[] { };

        [TestCase]
        public void ServiceCommandsWithNoRepos()
        {
            this.RunServiceCommandAndCheckOutput("--unmount-all", EmptyRepoList);
            this.RunServiceCommandAndCheckOutput("--mount-all", EmptyRepoList);
            this.RunServiceCommandAndCheckOutput("--list-mounted", EmptyRepoList);
        }

        [TestCase]
        public void ServiceCommandsWithMultipleRepos()
        {
            GSDFunctionalTestEnlistment enlistment1 = this.CreateNewEnlistment();
            GSDFunctionalTestEnlistment enlistment2 = this.CreateNewEnlistment();

            string[] repoRootList = new string[] { enlistment1.EnlistmentRoot, enlistment2.EnlistmentRoot };

            GSDProcess gvfsProcess1 = new GSDProcess(
                GSDTestConfig.PathToGSD,
                enlistment1.EnlistmentRoot,
                enlistment1.LocalCacheRoot);

            GSDProcess gvfsProcess2 = new GSDProcess(
                GSDTestConfig.PathToGSD,
                enlistment2.EnlistmentRoot,
                enlistment2.LocalCacheRoot);

            this.RunServiceCommandAndCheckOutput("--list-mounted", expectedRepoRoots: repoRootList);
            this.RunServiceCommandAndCheckOutput("--unmount-all", expectedRepoRoots: repoRootList);

            // Check both are unmounted
            gvfsProcess1.IsEnlistmentMounted().ShouldEqual(false);
            gvfsProcess2.IsEnlistmentMounted().ShouldEqual(false);

            this.RunServiceCommandAndCheckOutput("--list-mounted", EmptyRepoList);
            this.RunServiceCommandAndCheckOutput("--unmount-all", EmptyRepoList);
            this.RunServiceCommandAndCheckOutput("--mount-all", expectedRepoRoots: repoRootList);

            // Check both are mounted
            gvfsProcess1.IsEnlistmentMounted().ShouldEqual(true);
            gvfsProcess2.IsEnlistmentMounted().ShouldEqual(true);

            this.RunServiceCommandAndCheckOutput("--list-mounted", expectedRepoRoots: repoRootList);
        }

        [TestCase]
        public void ServiceCommandsWithMountAndUnmount()
        {
            GSDFunctionalTestEnlistment enlistment1 = this.CreateNewEnlistment();

            string[] repoRootList = new string[] { enlistment1.EnlistmentRoot };

            GSDProcess gvfsProcess1 = new GSDProcess(
                GSDTestConfig.PathToGSD,
                enlistment1.EnlistmentRoot,
                enlistment1.LocalCacheRoot);

            this.RunServiceCommandAndCheckOutput("--list-mounted", expectedRepoRoots: repoRootList);

            gvfsProcess1.Unmount();

            this.RunServiceCommandAndCheckOutput("--list-mounted", EmptyRepoList, unexpectedRepoRoots: repoRootList);
            this.RunServiceCommandAndCheckOutput("--unmount-all", EmptyRepoList, unexpectedRepoRoots: repoRootList);
            this.RunServiceCommandAndCheckOutput("--mount-all", EmptyRepoList, unexpectedRepoRoots: repoRootList);

            // Check that it is still unmounted
            gvfsProcess1.IsEnlistmentMounted().ShouldEqual(false);

            gvfsProcess1.Mount();

            this.RunServiceCommandAndCheckOutput("--unmount-all", expectedRepoRoots: repoRootList);
            this.RunServiceCommandAndCheckOutput("--mount-all", expectedRepoRoots: repoRootList);
        }

        private void RunServiceCommandAndCheckOutput(string argument, string[] expectedRepoRoots, string[] unexpectedRepoRoots = null)
        {
            GSDProcess gvfsProcess = new GSDProcess(
                GSDTestConfig.PathToGSD,
                enlistmentRoot: null,
                localCacheRoot: null);

            string result = gvfsProcess.RunServiceVerb(argument);
            result.ShouldContain(expectedRepoRoots);

            if (unexpectedRepoRoots != null)
            {
                result.ShouldNotContain(false, unexpectedRepoRoots);
            }
        }
    }
}
