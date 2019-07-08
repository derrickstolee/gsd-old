﻿using GSD.FunctionalTests.FileSystemRunners;
using GSD.FunctionalTests.Should;
using GSD.FunctionalTests.Tools;
using GSD.Tests.Should;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace GSD.FunctionalTests.Tests.EnlistmentPerFixture
{
    [TestFixture]
    [Category(Categories.WindowsOnly)]
    [Category(Categories.GitCommands)]
    public class WindowsTombstoneTests : TestsWithEnlistmentPerFixture
    {
        private const string Delimiter = "\r\n";
        private const int TombstoneFolderPlaceholderType = 3;
        private FileSystemRunner fileSystem;

        public WindowsTombstoneTests()
        {
            this.fileSystem = new SystemIORunner();
        }

        [TestCase]
        public void CheckoutCleansUpTombstones()
        {
            const string folderToDelete = "Scripts";

            // Delete directory to create the tombstone
            string directoryToDelete = this.Enlistment.GetVirtualPathTo(folderToDelete);
            this.fileSystem.DeleteDirectory(directoryToDelete);
            this.Enlistment.UnmountGSD();

            // Remove the directory entry from modified paths so git will not keep the folder up to date
            string modifiedPathsFile = Path.Combine(this.Enlistment.DotGSDRoot, TestConstants.Databases.ModifiedPaths);
            string modifiedPathsContent = this.fileSystem.ReadAllText(modifiedPathsFile);
            modifiedPathsContent = string.Join(Delimiter, modifiedPathsContent.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries).Where(x => !x.StartsWith($"A {folderToDelete}/")));
            this.fileSystem.WriteAllText(modifiedPathsFile, modifiedPathsContent + Delimiter);

            // Add tombstone folder entry to the placeholder database so the checkout will remove the tombstone
            // and start projecting the folder again
            string placeholderDatabasePath = Path.Combine(this.Enlistment.DotGSDRoot, TestConstants.Databases.GSD);
            GSDHelpers.AddPlaceholderFolder(placeholderDatabasePath, folderToDelete, TombstoneFolderPlaceholderType);

            this.Enlistment.MountGSD();
            directoryToDelete.ShouldNotExistOnDisk(this.fileSystem);

            // checkout branch to remove tombstones and project the folder again
            GitHelpers.InvokeGitAgainstGSDRepo(this.Enlistment.RepoRoot, "checkout -f HEAD");
            directoryToDelete.ShouldBeADirectory(this.fileSystem);

            this.Enlistment.UnmountGSD();

            string placholders = GSDHelpers.GetAllSQLitePlaceholdersAsString(placeholderDatabasePath);
            placholders.ShouldNotContain(ignoreCase: false, unexpectedSubstrings: $"{folderToDelete}{GSDHelpers.PlaceholderFieldDelimiter}{TombstoneFolderPlaceholderType}{GSDHelpers.PlaceholderFieldDelimiter}");
        }
    }
}
