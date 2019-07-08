using GSD.Common;
using GSD.UnitTests.Mock.Common;
using NUnit.Framework;

namespace GSD.UnitTests.Virtual
{
    [TestFixture]
    public abstract class TestsWithCommonRepo
    {
        protected CommonRepoSetup Repo { get; private set; }

        [SetUp]
        public virtual void TestSetup()
        {
            this.Repo = new CommonRepoSetup();

            string error;
            RepoMetadata.TryInitialize(
                new MockTracer(),
                this.Repo.Context.FileSystem,
                this.Repo.Context.Enlistment.DotGSDRoot,
                out error);
        }

        [TearDown]
        public virtual void TestTearDown()
        {
            if (this.Repo != null)
            {
                this.Repo.Dispose();
            }

            RepoMetadata.Shutdown();
        }
    }
}
