using GSD.FunctionalTests.Should;
using GSD.FunctionalTests.Tools;
using GSD.Tests.Should;
using NUnit.Framework;
using System;
using System.IO;

namespace GSD.FunctionalTests.Tests.EnlistmentPerTestCase
{
    [TestFixture]
    [Category(Categories.ExtraCoverage)]
    [Category(Categories.MacOnly)]
    public class MacDiskLayoutUpgradeTests : DiskLayoutUpgradeTests
    {
        public const int CurrentDiskLayoutMajorVersion = 19;
        public const int CurrentDiskLayoutMinorVersion = 0;

        public override int GetCurrentDiskLayoutMajorVersion() => CurrentDiskLayoutMajorVersion;
        public override int GetCurrentDiskLayoutMinorVersion() => CurrentDiskLayoutMinorVersion;

        [TestCase]
        public void MountUpgradesPlaceholderListDatabaseToSQLite()
        {
            this.Enlistment.UnmountGSD();

            this.fileSystem.DeleteFile(Path.Combine(this.Enlistment.DotGSDRoot, TestConstants.Databases.VFSForGit));
            this.WriteOldPlaceholderListDatabase();

            GSDHelpers.SaveDiskLayoutVersion(this.Enlistment.DotGSDRoot, "18", "0");

            this.Enlistment.MountGSD();
            this.Enlistment.UnmountGSD();

            // Validate the placeholders are in the SQLite placeholder database now
            string placeholderDatabasePath = Path.Combine(this.Enlistment.DotGSDRoot, TestConstants.Databases.VFSForGit);
            placeholderDatabasePath.ShouldBeAFile(this.fileSystem);
            string[] lines = GSDHelpers.GetAllSQLitePlaceholdersAsString(placeholderDatabasePath).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            lines.Length.ShouldEqual(10);
            this.PlaceholderDatabaseShouldIncludeCommonLines(lines);
            lines.ShouldContain(x => x == this.PartialFolderPlaceholderString("GSD", "GSD.Tests", "Properties"));

            this.ValidatePersistedVersionMatchesCurrentVersion();
        }
    }
}
