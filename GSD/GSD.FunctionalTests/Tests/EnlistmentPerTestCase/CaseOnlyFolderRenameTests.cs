using GSD.FunctionalTests.FileSystemRunners;
using GSD.FunctionalTests.Should;
using GSD.FunctionalTests.Tools;
using GSD.Tests.Should;
using NUnit.Framework;
using System.IO;

namespace GSD.FunctionalTests.Tests.EnlistmentPerTestCase
{
    [TestFixture]
    public class CaseOnlyFolderRenameTests : TestsWithEnlistmentPerTestCase
    {
        private FileSystemRunner fileSystem;

        public CaseOnlyFolderRenameTests()
        {
            this.fileSystem = new BashRunner();
        }

        // MacOnly because renames of partial folders are blocked on Windows
        [TestCase]
        [Category(Categories.MacOnly)]
        public void CaseRenameFoldersAndRemountAndRenameAgain()
        {
            // Projected folder without a physical folder
            string parentFolderName = "GVFS";
            string oldGSDSubFolderName = "GVFS";
            string oldGSDSubFolderPath = Path.Combine(parentFolderName, oldGSDSubFolderName);
            string newGSDSubFolderName = "gvfs";
            string newGSDSubFolderPath = Path.Combine(parentFolderName, newGSDSubFolderName);

            this.Enlistment.GetVirtualPathTo(oldGSDSubFolderPath).ShouldBeADirectory(this.fileSystem).WithCaseMatchingName(oldGSDSubFolderName);

            this.fileSystem.MoveFile(this.Enlistment.GetVirtualPathTo(oldGSDSubFolderPath), this.Enlistment.GetVirtualPathTo(newGSDSubFolderPath));

            this.Enlistment.GetVirtualPathTo(newGSDSubFolderPath).ShouldBeADirectory(this.fileSystem).WithCaseMatchingName(newGSDSubFolderName);

            // Projected folder with a physical folder
            string oldTestsSubFolderName = "GVFS.FunctionalTests";
            string oldTestsSubFolderPath = Path.Combine(parentFolderName, oldTestsSubFolderName);
            string newTestsSubFolderName = "gvfs.functionaltests";
            string newTestsSubFolderPath = Path.Combine(parentFolderName, newTestsSubFolderName);

            string fileToAdd = "NewFile.txt";
            string fileToAddContent = "This is new file text.";
            string fileToAddPath = this.Enlistment.GetVirtualPathTo(Path.Combine(oldTestsSubFolderPath, fileToAdd));
            this.fileSystem.WriteAllText(fileToAddPath, fileToAddContent);

            this.Enlistment.GetVirtualPathTo(oldTestsSubFolderPath).ShouldBeADirectory(this.fileSystem).WithCaseMatchingName(oldTestsSubFolderName);

            this.fileSystem.MoveFile(this.Enlistment.GetVirtualPathTo(oldTestsSubFolderPath), this.Enlistment.GetVirtualPathTo(newTestsSubFolderPath));

            this.Enlistment.GetVirtualPathTo(newTestsSubFolderPath).ShouldBeADirectory(this.fileSystem).WithCaseMatchingName(newTestsSubFolderName);

            // Remount
            this.Enlistment.UnmountGSD();
            this.Enlistment.MountGSD();

            this.Enlistment.GetVirtualPathTo(newGSDSubFolderPath).ShouldBeADirectory(this.fileSystem).WithCaseMatchingName(newGSDSubFolderName);
            this.Enlistment.GetVirtualPathTo(newTestsSubFolderPath).ShouldBeADirectory(this.fileSystem).WithCaseMatchingName(newTestsSubFolderName);
            this.Enlistment.GetVirtualPathTo(Path.Combine(newTestsSubFolderPath, fileToAdd)).ShouldBeAFile(this.fileSystem).WithContents().ShouldEqual(fileToAddContent);

            // Rename each folder again
            string finalGSDSubFolderName = "gvFS";
            string finalGSDSubFolderPath = Path.Combine(parentFolderName, finalGSDSubFolderName);
            this.fileSystem.MoveFile(this.Enlistment.GetVirtualPathTo(newGSDSubFolderPath), this.Enlistment.GetVirtualPathTo(finalGSDSubFolderPath));
            this.Enlistment.GetVirtualPathTo(finalGSDSubFolderPath).ShouldBeADirectory(this.fileSystem).WithCaseMatchingName(finalGSDSubFolderName);

            string finalTestsSubFolderName = "gvfs.FunctionalTESTS";
            string finalTestsSubFolderPath = Path.Combine(parentFolderName, finalTestsSubFolderName);
            this.fileSystem.MoveFile(this.Enlistment.GetVirtualPathTo(newTestsSubFolderPath), this.Enlistment.GetVirtualPathTo(finalTestsSubFolderPath));
            this.Enlistment.GetVirtualPathTo(finalTestsSubFolderPath).ShouldBeADirectory(this.fileSystem).WithCaseMatchingName(finalTestsSubFolderName);
            this.Enlistment.GetVirtualPathTo(Path.Combine(finalTestsSubFolderPath, fileToAdd)).ShouldBeAFile(this.fileSystem).WithContents().ShouldEqual(fileToAddContent);
        }
    }
}
