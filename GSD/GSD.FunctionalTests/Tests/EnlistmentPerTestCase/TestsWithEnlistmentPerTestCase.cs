using GSD.FunctionalTests.Tools;
using NUnit.Framework;

namespace GSD.FunctionalTests.Tests.EnlistmentPerTestCase
{
    [TestFixture]
    public abstract class TestsWithEnlistmentPerTestCase
    {
        private readonly bool forcePerRepoObjectCache;

        public TestsWithEnlistmentPerTestCase(bool forcePerRepoObjectCache = false)
        {
            this.forcePerRepoObjectCache = forcePerRepoObjectCache;
        }

        public GSDFunctionalTestEnlistment Enlistment
        {
            get; private set;
        }

        [SetUp]
        public virtual void CreateEnlistment()
        {
            if (this.forcePerRepoObjectCache)
            {
                this.Enlistment = GSDFunctionalTestEnlistment.CloneAndMountWithPerRepoCache(GSDTestConfig.PathToGSD, skipPrefetch: false);
            }
            else
            {
                this.Enlistment = GSDFunctionalTestEnlistment.CloneAndMount(GSDTestConfig.PathToGSD);
            }
        }

        [TearDown]
        public virtual void DeleteEnlistment()
        {
            if (this.Enlistment != null)
            {
                this.Enlistment.UnmountAndDeleteAll();
            }
        }
    }
}
