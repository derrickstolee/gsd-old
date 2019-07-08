using GSD.FunctionalTests.Should;
using GSD.FunctionalTests.Tools;
using NUnit.Framework;
using System.IO;

namespace GSD.FunctionalTests.Tests.GitCommands
{
    [TestFixture]
    public class RmTests : GitRepoTests
    {
        public RmTests()
            : base(enlistmentPerTest: false, validateWorkingTree: false)
        {
        }

        [TestCase]
        public void CanReadFileAfterGitRmDryRun()
        {
            this.ValidateGitCommand("status");

            // Validate that Scripts\RunUnitTests.bad is not on disk at all
            string filePath = Path.Combine("Scripts", "RunUnitTests.bat");

            this.Enlistment.UnmountGSD();
            this.Enlistment.GetVirtualPathTo(filePath).ShouldNotExistOnDisk(this.FileSystem);
            this.Enlistment.MountGSD();

            this.ValidateGitCommand("rm --dry-run " + GitHelpers.ConvertPathToGitFormat(filePath));
            this.FileContentsShouldMatch(filePath);
        }
    }
}
