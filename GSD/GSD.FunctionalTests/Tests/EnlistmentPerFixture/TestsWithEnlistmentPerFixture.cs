using GSD.FunctionalTests.Tools;
using NUnit.Framework;

namespace GSD.FunctionalTests.Tests.EnlistmentPerFixture
{
    [TestFixture]
    public abstract class TestsWithEnlistmentPerFixture
    {
        private readonly bool forcePerRepoObjectCache;
        private readonly bool skipPrefetchDuringClone;

        public TestsWithEnlistmentPerFixture(bool forcePerRepoObjectCache = false, bool skipPrefetchDuringClone = false)
        {
            this.forcePerRepoObjectCache = forcePerRepoObjectCache;
            this.skipPrefetchDuringClone = skipPrefetchDuringClone;
        }

        public GSDFunctionalTestEnlistment Enlistment
        {
            get; private set;
        }

        [OneTimeSetUp]
        public virtual void CreateEnlistment()
        {
            if (this.forcePerRepoObjectCache)
            {
                this.Enlistment = GSDFunctionalTestEnlistment.CloneAndMountWithPerRepoCache(GSDTestConfig.PathToGSD, this.skipPrefetchDuringClone);
            }
            else
            {
                this.Enlistment = GSDFunctionalTestEnlistment.CloneAndMount(GSDTestConfig.PathToGSD);
            }
        }

        [OneTimeTearDown]
        public virtual void DeleteEnlistment()
        {
            if (this.Enlistment != null)
            {
                this.Enlistment.UnmountAndDeleteAll();
            }
        }
    }
}
