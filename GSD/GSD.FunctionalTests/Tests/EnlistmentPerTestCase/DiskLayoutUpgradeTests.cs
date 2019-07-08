using GSD.FunctionalTests.FileSystemRunners;
using GSD.FunctionalTests.Should;
using GSD.FunctionalTests.Tools;
using GSD.Tests.Should;
using System;
using System.IO;

namespace GSD.FunctionalTests.Tests.EnlistmentPerTestCase
{
    public abstract class DiskLayoutUpgradeTests : TestsWithEnlistmentPerTestCase
    {
        protected static readonly string PlaceholderListDatabaseContent = $@"A .gitignore{GSDHelpers.PlaceholderFieldDelimiter}E9630E4CF715315FC90D4AEC98E16A7398F8BF64
A Readme.md{GSDHelpers.PlaceholderFieldDelimiter}583F1A56DB7CC884D54534C5D9C56B93A1E00A2B
A Scripts{GSDHelpers.PlaceholderFieldDelimiter}{TestConstants.PartialFolderPlaceholderDatabaseValue}
A Scripts{Path.DirectorySeparatorChar}RunUnitTests.bat{GSDHelpers.PlaceholderFieldDelimiter}0112E0DD6FC64BF57C4735F4D7D6E018C0F34B6D
A GSD{GSDHelpers.PlaceholderFieldDelimiter}{TestConstants.PartialFolderPlaceholderDatabaseValue}
A GSD{Path.DirectorySeparatorChar}GSD.Common{GSDHelpers.PlaceholderFieldDelimiter}{TestConstants.PartialFolderPlaceholderDatabaseValue}
A GSD{Path.DirectorySeparatorChar}GSD.Common{Path.DirectorySeparatorChar}Git{GSDHelpers.PlaceholderFieldDelimiter}{TestConstants.PartialFolderPlaceholderDatabaseValue}
A GSD{Path.DirectorySeparatorChar}GSD.Common{Path.DirectorySeparatorChar}Git{Path.DirectorySeparatorChar}GitRefs.cs{GSDHelpers.PlaceholderFieldDelimiter}37595A9C6C7E00A8AFDE306765896770F2508927
A GSD{Path.DirectorySeparatorChar}GSD.Tests{GSDHelpers.PlaceholderFieldDelimiter}{TestConstants.PartialFolderPlaceholderDatabaseValue}
A GSD{Path.DirectorySeparatorChar}GSD.Tests{Path.DirectorySeparatorChar}Properties{GSDHelpers.PlaceholderFieldDelimiter}{TestConstants.PartialFolderPlaceholderDatabaseValue}
A GSD{Path.DirectorySeparatorChar}GSD.Tests{Path.DirectorySeparatorChar}Properties{Path.DirectorySeparatorChar}AssemblyInfo.cs{GSDHelpers.PlaceholderFieldDelimiter}5911485CFE87E880F64B300BA5A289498622DBC1
D GSD{Path.DirectorySeparatorChar}GSD.Tests{Path.DirectorySeparatorChar}Properties{Path.DirectorySeparatorChar}AssemblyInfo.cs
";

        protected FileSystemRunner fileSystem = new SystemIORunner();

        private const string PlaceholderTableFilePathType = "0";
        private const string PlaceholderTablePartialFolderPathType = "1";

        public abstract int GetCurrentDiskLayoutMajorVersion();
        public abstract int GetCurrentDiskLayoutMinorVersion();

        protected void PlaceholderDatabaseShouldIncludeCommonLines(string[] placeholderLines)
        {
            placeholderLines.ShouldContain(x => x.Contains(this.FilePlaceholderString("Readme.md")));
            placeholderLines.ShouldContain(x => x.Contains(this.FilePlaceholderString("Scripts", "RunUnitTests.bat")));
            placeholderLines.ShouldContain(x => x.Contains(this.FilePlaceholderString("GSD", "GSD.Common", "Git", "GitRefs.cs")));
            placeholderLines.ShouldContain(x => x.Contains(this.FilePlaceholderString(".gitignore")));
            placeholderLines.ShouldContain(x => x == this.PartialFolderPlaceholderString("Scripts"));
            placeholderLines.ShouldContain(x => x == this.PartialFolderPlaceholderString("GSD"));
            placeholderLines.ShouldContain(x => x == this.PartialFolderPlaceholderString("GSD", "GSD.Common"));
            placeholderLines.ShouldContain(x => x == this.PartialFolderPlaceholderString("GSD", "GSD.Common", "Git"));
            placeholderLines.ShouldContain(x => x == this.PartialFolderPlaceholderString("GSD", "GSD.Tests"));
        }

        protected void WriteOldPlaceholderListDatabase()
        {
            this.fileSystem.WriteAllText(Path.Combine(this.Enlistment.DotGSDRoot, GSDHelpers.PlaceholderListFile), PlaceholderListDatabaseContent);
        }

        protected void PerformIOBeforePlaceholderDatabaseUpgradeTest()
        {
            // Create some placeholder data
            this.fileSystem.ReadAllText(Path.Combine(this.Enlistment.RepoRoot, "Readme.md"));
            this.fileSystem.ReadAllText(Path.Combine(this.Enlistment.RepoRoot, "Scripts", "RunUnitTests.bat"));
            this.fileSystem.ReadAllText(Path.Combine(this.Enlistment.RepoRoot, "GSD", "GSD.Common", "Git", "GitRefs.cs"));

            // Create a full folder
            this.fileSystem.CreateDirectory(Path.Combine(this.Enlistment.RepoRoot, "GSD", "FullFolder"));
            this.fileSystem.WriteAllText(Path.Combine(this.Enlistment.RepoRoot, "GSD", "FullFolder", "test.txt"), "Test contents");

            // Create a tombstone
            this.fileSystem.DeleteDirectory(Path.Combine(this.Enlistment.RepoRoot, "GSD", "GSD.Tests", "Properties"));

            string junctionTarget = Path.Combine(this.Enlistment.EnlistmentRoot, "DirJunction");
            string symLinkTarget = Path.Combine(this.Enlistment.EnlistmentRoot, "DirSymLink");
            Directory.CreateDirectory(junctionTarget);
            Directory.CreateDirectory(symLinkTarget);

            string junctionLink = Path.Combine(this.Enlistment.RepoRoot, "DirJunction");
            string symLink = Path.Combine(this.Enlistment.RepoRoot, "DirLink");
            ProcessHelper.Run("CMD.exe", "/C mklink /J " + junctionLink + " " + junctionTarget);
            ProcessHelper.Run("CMD.exe", "/C mklink /D " + symLink + " " + symLinkTarget);

            string target = Path.Combine(this.Enlistment.EnlistmentRoot, "GSD", "GSD", "GSD.UnitTests");
            string link = Path.Combine(this.Enlistment.RepoRoot, "UnitTests");
            ProcessHelper.Run("CMD.exe", "/C mklink /J " + link + " " + target);
            target = Path.Combine(this.Enlistment.EnlistmentRoot, "GSD", "GSD", "GSD.Installer");
            link = Path.Combine(this.Enlistment.RepoRoot, "Installer");
            ProcessHelper.Run("CMD.exe", "/C mklink /D " + link + " " + target);
        }

        protected string FilePlaceholderString(params string[] pathParts)
        {
            return $"{Path.Combine(pathParts)}{GSDHelpers.PlaceholderFieldDelimiter}{PlaceholderTableFilePathType}{GSDHelpers.PlaceholderFieldDelimiter}";
        }

        protected string PartialFolderPlaceholderString(params string[] pathParts)
        {
            return $"{Path.Combine(pathParts)}{GSDHelpers.PlaceholderFieldDelimiter}{PlaceholderTablePartialFolderPathType}{GSDHelpers.PlaceholderFieldDelimiter}";
        }

        protected void ValidatePersistedVersionMatchesCurrentVersion()
        {
            string majorVersion;
            string minorVersion;
            GSDHelpers.GetPersistedDiskLayoutVersion(this.Enlistment.DotGSDRoot, out majorVersion, out minorVersion);

            majorVersion
                .ShouldBeAnInt("Disk layout version should always be an int")
                .ShouldEqual(this.GetCurrentDiskLayoutMajorVersion(), "Disk layout version should be upgraded to the latest");

            minorVersion
                .ShouldBeAnInt("Disk layout version should always be an int")
                .ShouldEqual(this.GetCurrentDiskLayoutMinorVersion(), "Disk layout version should be upgraded to the latest");
        }
    }
}
