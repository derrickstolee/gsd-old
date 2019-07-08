using GSD.FunctionalTests.Should;
using GSD.FunctionalTests.Tools;
using NUnit.Framework;
using System.IO;

namespace GSD.FunctionalTests.Tests.GitCommands
{
    [TestFixture]
    [Category(Categories.GitCommands)]
    public class HashObjectTests : GitRepoTests
    {
        public HashObjectTests()
            : base(enlistmentPerTest: false, validateWorkingTree: false)
        {
        }

        [TestCase]
        public void CanReadFileAfterHashObject()
        {
            this.ValidateGitCommand("status");

            // Validate that Scripts\RunUnitTests.bad is not on disk at all
            string filePath = Path.Combine("Scripts", "RunUnitTests.bat");

            this.Enlistment.UnmountGSD();
            this.Enlistment.GetVirtualPathTo(filePath).ShouldNotExistOnDisk(this.FileSystem);
            this.Enlistment.MountGSD();

            // TODO 1087312: Fix 'git hash-oject' so that it works for files that aren't on disk yet
            GitHelpers.InvokeGitAgainstGSDRepo(
                this.Enlistment.RepoRoot,
                "hash-object " + GitHelpers.ConvertPathToGitFormat(filePath));

            this.FileContentsShouldMatch(filePath);
        }
    }
}
