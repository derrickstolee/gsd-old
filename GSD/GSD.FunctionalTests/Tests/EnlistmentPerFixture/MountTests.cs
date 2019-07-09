using GSD.FunctionalTests.FileSystemRunners;
using GSD.FunctionalTests.Properties;
using GSD.FunctionalTests.Should;
using GSD.FunctionalTests.Tools;
using GSD.Tests.Should;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;

namespace GSD.FunctionalTests.Tests.EnlistmentPerFixture
{
    [TestFixture]
    [Category(Categories.ExtraCoverage)]
    public class MountTests : TestsWithEnlistmentPerFixture
    {
        private const int GSDGenericError = 3;

        private FileSystemRunner fileSystem;

        public MountTests()
        {
            this.fileSystem = new SystemIORunner();
        }

        [TestCaseSource(typeof(MountSubfolders), MountSubfolders.MountFolders)]
        public void SecondMountAttemptFails(string mountSubfolder)
        {
            this.MountShouldFail(0, "already mounted", this.Enlistment.GetVirtualPathTo(mountSubfolder));
        }

        [TestCase]
        public void MountFailsOutsideEnlistment()
        {
            this.MountShouldFail("is not a valid GSD enlistment", Path.GetDirectoryName(this.Enlistment.EnlistmentRoot));
        }

        [TestCase]
        public void MountCopiesMissingReadObjectHook()
        {
            this.Enlistment.UnmountGSD();

            string readObjectPath = this.Enlistment.GetVirtualPathTo(".git", "hooks", "read-object" + Settings.Default.BinaryFileNameExtension);
            readObjectPath.ShouldBeAFile(this.fileSystem);
            this.fileSystem.DeleteFile(readObjectPath);
            readObjectPath.ShouldNotExistOnDisk(this.fileSystem);
            this.Enlistment.MountGSD();
            readObjectPath.ShouldBeAFile(this.fileSystem);
        }

        [TestCase]
        public void MountSetsCoreHooksPath()
        {
            this.Enlistment.UnmountGSD();

            GitProcess.Invoke(this.Enlistment.RepoRoot, "config --unset core.hookspath");
            string.IsNullOrWhiteSpace(
                GitProcess.Invoke(this.Enlistment.RepoRoot, "config core.hookspath"))
                .ShouldBeTrue();

            this.Enlistment.MountGSD();
            string expectedHooksPath = Path.Combine(this.Enlistment.RepoRoot, ".git", "hooks");
            expectedHooksPath = GitHelpers.ConvertPathToGitFormat(expectedHooksPath);

            GitProcess.Invoke(
                this.Enlistment.RepoRoot, "config core.hookspath")
                .Trim('\n')
                .ShouldEqual(expectedHooksPath);
        }

        [TestCase]
        public void MountChangesMountId()
        {
            string mountId = GitProcess.Invoke(this.Enlistment.RepoRoot, "config gvfs.mount-id")
                .Trim('\n');
            this.Enlistment.UnmountGSD();
            this.Enlistment.MountGSD();
            GitProcess.Invoke(this.Enlistment.RepoRoot, "config gvfs.mount-id")
                .Trim('\n')
                .ShouldNotEqual(mountId, "gvfs.mount-id should change on every mount");
        }

        [TestCase]
        public void MountFailsWhenNoOnDiskVersion()
        {
            this.Enlistment.UnmountGSD();

            // Get the current disk layout version
            string majorVersion;
            string minorVersion;
            GSDHelpers.GetPersistedDiskLayoutVersion(this.Enlistment.DotGSDRoot, out majorVersion, out minorVersion);

            int majorVersionNum;
            int minorVersionNum;
            int.TryParse(majorVersion.ShouldNotBeNull(), out majorVersionNum).ShouldEqual(true);
            int.TryParse(minorVersion.ShouldNotBeNull(), out minorVersionNum).ShouldEqual(true);

            // Move the RepoMetadata database to a temp file
            string versionDatabasePath = Path.Combine(this.Enlistment.DotGSDRoot, GSDHelpers.RepoMetadataName);
            versionDatabasePath.ShouldBeAFile(this.fileSystem);

            string tempDatabasePath = versionDatabasePath + "_MountFailsWhenNoOnDiskVersion";
            tempDatabasePath.ShouldNotExistOnDisk(this.fileSystem);

            this.fileSystem.MoveFile(versionDatabasePath, tempDatabasePath);
            versionDatabasePath.ShouldNotExistOnDisk(this.fileSystem);

            this.MountShouldFail("Failed to upgrade repo disk layout");

            // Move the RepoMetadata database back
            this.fileSystem.DeleteFile(versionDatabasePath);
            this.fileSystem.MoveFile(tempDatabasePath, versionDatabasePath);
            tempDatabasePath.ShouldNotExistOnDisk(this.fileSystem);
            versionDatabasePath.ShouldBeAFile(this.fileSystem);

            this.Enlistment.MountGSD();
        }

        [TestCase]
        public void MountFailsWhenNoLocalCacheRootInRepoMetadata()
        {
            this.Enlistment.UnmountGSD();

            string majorVersion;
            string minorVersion;
            GSDHelpers.GetPersistedDiskLayoutVersion(this.Enlistment.DotGSDRoot, out majorVersion, out minorVersion);
            majorVersion.ShouldNotBeNull();
            minorVersion.ShouldNotBeNull();

            string objectsRoot = GSDHelpers.GetPersistedGitObjectsRoot(this.Enlistment.DotGSDRoot).ShouldNotBeNull();

            string metadataPath = Path.Combine(this.Enlistment.DotGSDRoot, GSDHelpers.RepoMetadataName);
            string metadataBackupPath = metadataPath + ".backup";
            this.fileSystem.MoveFile(metadataPath, metadataBackupPath);

            this.fileSystem.CreateEmptyFile(metadataPath);
            GSDHelpers.SaveDiskLayoutVersion(this.Enlistment.DotGSDRoot, majorVersion, minorVersion);
            GSDHelpers.SaveGitObjectsRoot(this.Enlistment.DotGSDRoot, objectsRoot);

            this.MountShouldFail("Failed to determine local cache path from repo metadata");

            this.fileSystem.DeleteFile(metadataPath);
            this.fileSystem.MoveFile(metadataBackupPath, metadataPath);

            this.Enlistment.MountGSD();
        }

        [TestCase]
        public void MountFailsWhenNoGitObjectsRootInRepoMetadata()
        {
            this.Enlistment.UnmountGSD();

            string majorVersion;
            string minorVersion;
            GSDHelpers.GetPersistedDiskLayoutVersion(this.Enlistment.DotGSDRoot, out majorVersion, out minorVersion);
            majorVersion.ShouldNotBeNull();
            minorVersion.ShouldNotBeNull();

            string localCacheRoot = GSDHelpers.GetPersistedLocalCacheRoot(this.Enlistment.DotGSDRoot).ShouldNotBeNull();

            string metadataPath = Path.Combine(this.Enlistment.DotGSDRoot, GSDHelpers.RepoMetadataName);
            string metadataBackupPath = metadataPath + ".backup";
            this.fileSystem.MoveFile(metadataPath, metadataBackupPath);

            this.fileSystem.CreateEmptyFile(metadataPath);
            GSDHelpers.SaveDiskLayoutVersion(this.Enlistment.DotGSDRoot, majorVersion, minorVersion);
            GSDHelpers.SaveLocalCacheRoot(this.Enlistment.DotGSDRoot, localCacheRoot);

            this.MountShouldFail("Failed to determine git objects root from repo metadata");

            this.fileSystem.DeleteFile(metadataPath);
            this.fileSystem.MoveFile(metadataBackupPath, metadataPath);

            this.Enlistment.MountGSD();
        }

        [TestCase]
        public void MountRegeneratesAlternatesFileWhenMissingGitObjectsRoot()
        {
            this.Enlistment.UnmountGSD();

            string objectsRoot = GSDHelpers.GetPersistedGitObjectsRoot(this.Enlistment.DotGSDRoot).ShouldNotBeNull();

            string alternatesFilePath = Path.Combine(this.Enlistment.RepoRoot, ".git", "objects", "info", "alternates");
            alternatesFilePath.ShouldBeAFile(this.fileSystem).WithContents(objectsRoot);
            this.fileSystem.WriteAllText(alternatesFilePath, "Z:\\invalidPath");

            this.Enlistment.MountGSD();

            alternatesFilePath.ShouldBeAFile(this.fileSystem).WithContents(objectsRoot);
        }

        [TestCase]
        public void MountRegeneratesAlternatesFileWhenMissingFromDisk()
        {
            this.Enlistment.UnmountGSD();

            string objectsRoot = GSDHelpers.GetPersistedGitObjectsRoot(this.Enlistment.DotGSDRoot).ShouldNotBeNull();

            string alternatesFilePath = Path.Combine(this.Enlistment.RepoRoot, ".git", "objects", "info", "alternates");
            alternatesFilePath.ShouldBeAFile(this.fileSystem).WithContents(objectsRoot);
            this.fileSystem.DeleteFile(alternatesFilePath);

            this.Enlistment.MountGSD();

            alternatesFilePath.ShouldBeAFile(this.fileSystem).WithContents(objectsRoot);
        }

        [TestCaseSource(typeof(MountSubfolders), MountSubfolders.MountFolders)]
        public void MountFailsAfterBreakingDowngrade(string mountSubfolder)
        {
            MountSubfolders.EnsureSubfoldersOnDisk(this.Enlistment, this.fileSystem);
            this.Enlistment.UnmountGSD();

            string majorVersion;
            string minorVersion;
            GSDHelpers.GetPersistedDiskLayoutVersion(this.Enlistment.DotGSDRoot, out majorVersion, out minorVersion);

            int majorVersionNum;
            int minorVersionNum;
            int.TryParse(majorVersion.ShouldNotBeNull(), out majorVersionNum).ShouldEqual(true);
            int.TryParse(minorVersion.ShouldNotBeNull(), out minorVersionNum).ShouldEqual(true);

            GSDHelpers.SaveDiskLayoutVersion(this.Enlistment.DotGSDRoot, (majorVersionNum + 1).ToString(), "0");

            this.MountShouldFail("do not allow mounting after downgrade", this.Enlistment.GetVirtualPathTo(mountSubfolder));

            GSDHelpers.SaveDiskLayoutVersion(this.Enlistment.DotGSDRoot, majorVersionNum.ToString(), minorVersionNum.ToString());
            this.Enlistment.MountGSD();
        }

        [TestCaseSource(typeof(MountSubfolders), MountSubfolders.MountFolders)]
        public void MountFailsUpgradingFromInvalidUpgradePath(string mountSubfolder)
        {
            MountSubfolders.EnsureSubfoldersOnDisk(this.Enlistment, this.fileSystem);
            string headCommitId = GitProcess.Invoke(this.Enlistment.RepoRoot, "rev-parse HEAD");

            this.Enlistment.UnmountGSD();

            string majorVersion;
            string minorVersion;
            GSDHelpers.GetPersistedDiskLayoutVersion(this.Enlistment.DotGSDRoot, out majorVersion, out minorVersion);

            int majorVersionNum;
            int minorVersionNum;
            int.TryParse(majorVersion.ShouldNotBeNull(), out majorVersionNum).ShouldEqual(true);
            int.TryParse(minorVersion.ShouldNotBeNull(), out minorVersionNum).ShouldEqual(true);

            // 1 will always be below the minumum support version number
            GSDHelpers.SaveDiskLayoutVersion(this.Enlistment.DotGSDRoot, "1", "0");
            this.MountShouldFail("Breaking change to GSD disk layout has been made since cloning", this.Enlistment.GetVirtualPathTo(mountSubfolder));

            GSDHelpers.SaveDiskLayoutVersion(this.Enlistment.DotGSDRoot, majorVersionNum.ToString(), minorVersionNum.ToString());
            this.Enlistment.MountGSD();
        }

        private void MountShouldFail(int expectedExitCode, string expectedErrorMessage, string mountWorkingDirectory = null)
        {
            string enlistmentRoot = this.Enlistment.EnlistmentRoot;

            // TODO: 865304 Use app.config instead of --internal* arguments
            ProcessStartInfo processInfo = new ProcessStartInfo(GSDTestConfig.PathToGSD);
            processInfo.Arguments = "mount " + TestConstants.InternalUseOnlyFlag + " " + GSDHelpers.GetInternalParameter();
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.WorkingDirectory = string.IsNullOrEmpty(mountWorkingDirectory) ? enlistmentRoot : mountWorkingDirectory;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;

            ProcessResult result = ProcessHelper.Run(processInfo);
            result.ExitCode.ShouldEqual(expectedExitCode, $"mount exit code was not {expectedExitCode}. Output: {result.Output}");
            result.Output.ShouldContain(expectedErrorMessage);
        }

        private void MountShouldFail(string expectedErrorMessage, string mountWorkingDirectory = null)
        {
            this.MountShouldFail(GSDGenericError, expectedErrorMessage, mountWorkingDirectory);
        }

        private class MountSubfolders
        {
            public const string MountFolders = "Folders";

            public static object[] Folders
            {
                get
                {
                    return Array.Empty<object>();
                }
            }

            public static void EnsureSubfoldersOnDisk(GSDFunctionalTestEnlistment enlistment, FileSystemRunner fileSystem)
            {
                // Enumerate the directory to ensure that the folder is on disk after GSD is unmounted
                foreach (object[] folder in Folders)
                {
                    string folderPath = enlistment.GetVirtualPathTo((string)folder[0]);
                    folderPath.ShouldBeADirectory(fileSystem).WithItems();
                }
            }
        }
    }
}
