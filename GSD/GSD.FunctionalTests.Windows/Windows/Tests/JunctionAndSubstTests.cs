using GSD.FunctionalTests.FileSystemRunners;
using GSD.FunctionalTests.Should;
using GSD.FunctionalTests.Tests.EnlistmentPerFixture;
using GSD.FunctionalTests.Tools;
using GSD.Tests.Should;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GSD.FunctionalTests.Windows.Tests
{
    [TestFixture]
    [Category(Categories.ExtraCoverage)]
    [Category(Categories.WindowsOnly)]
    public class JunctionAndSubstTests : TestsWithEnlistmentPerFixture
    {
        private const string SubstDrive = "Q:";
        private const string SubstDrivePath = @"Q:\";
        private const string JunctionAndSubstTestsName = nameof(JunctionAndSubstTests);
        private const string ExpectedStatusWaitingText = @"Waiting for 'GSD.FunctionalTests.LockHolder'";

        private string junctionsRoot;
        private FileSystemRunner fileSystem;

        public JunctionAndSubstTests()
        {
            this.fileSystem = new SystemIORunner();
        }

        [SetUp]
        public void SetupJunctionRoot()
        {
            // Create junctionsRoot in Properties.Settings.Default.EnlistmentRoot (the parent folder of the GSD enlistment root `this.Enlistment.EnlistmentRoot`)
            // junctionsRoot is created here (outside of the GSD enlistment root) to ensure that git hooks and GSD commands will not find a .gvfs folder
            // walking up the tree from their current (non-normalized) path.
            this.junctionsRoot = Path.Combine(Properties.Settings.Default.EnlistmentRoot, JunctionAndSubstTestsName, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.junctionsRoot);
        }

        [TearDown]
        public void TearDownJunctionRoot()
        {
            DirectoryInfo junctionsRootInfo = new DirectoryInfo(this.junctionsRoot);
            if (junctionsRootInfo.Exists)
            {
                foreach (DirectoryInfo junction in junctionsRootInfo.GetDirectories())
                {
                    junction.Delete();
                }

                junctionsRootInfo.Delete();
            }
        }

        [TestCase]
        public void GSDStatusWorksFromSubstDrive()
        {
            this.CreateSubstDrive(this.Enlistment.EnlistmentRoot);
            this.RepoStatusShouldBeMounted(workingDirectory: SubstDrivePath);

            this.CreateSubstDrive(this.Enlistment.RepoRoot);
            this.RepoStatusShouldBeMounted(workingDirectory: SubstDrivePath);

            string subFolderPath = this.Enlistment.GetVirtualPathTo("GSD");
            this.CreateSubstDrive(subFolderPath);
            this.RepoStatusShouldBeMounted(workingDirectory: SubstDrivePath);

            this.RepoStatusShouldBeMounted(workingDirectory: string.Empty, enlistmentPath: SubstDrive);
        }

        [TestCase]
        public void GSDStatusWorksFromJunction()
        {
            string enlistmentRootjunctionLink = Path.Combine(this.junctionsRoot, $"{nameof(this.GSDStatusWorksFromJunction)}_ToEnlistmentRoot");
            this.CreateJunction(enlistmentRootjunctionLink, this.Enlistment.EnlistmentRoot);
            this.RepoStatusShouldBeMounted(workingDirectory: enlistmentRootjunctionLink);

            string junctionLink = Path.Combine(this.junctionsRoot, $"{nameof(this.GSDStatusWorksFromJunction)}_ToRepoRoot");
            this.CreateJunction(junctionLink, this.Enlistment.RepoRoot);
            this.RepoStatusShouldBeMounted(workingDirectory: junctionLink);

            junctionLink = Path.Combine(this.junctionsRoot, $"{nameof(this.GSDStatusWorksFromJunction)}_ToSubFolder");
            string subFolderPath = this.Enlistment.GetVirtualPathTo("GSD");
            this.CreateJunction(junctionLink, subFolderPath);
            this.RepoStatusShouldBeMounted(workingDirectory: junctionLink);

            this.RepoStatusShouldBeMounted(workingDirectory: string.Empty, enlistmentPath: enlistmentRootjunctionLink);
        }

        [TestCase]
        public void GSDMountWorksFromSubstDrive()
        {
            this.CreateSubstDrive(this.Enlistment.EnlistmentRoot);
            this.Enlistment.UnmountGSD();
            this.MountGSD(workingDirectory: SubstDrivePath);

            this.CreateSubstDrive(this.Enlistment.RepoRoot);
            this.Enlistment.UnmountGSD();
            this.MountGSD(workingDirectory: SubstDrivePath);

            string subFolderPath = this.Enlistment.GetVirtualPathTo("GSD");
            subFolderPath.ShouldBeADirectory(this.fileSystem);
            this.CreateSubstDrive(subFolderPath);
            this.Enlistment.UnmountGSD();
            this.MountGSD(workingDirectory: SubstDrivePath);

            this.Enlistment.UnmountGSD();
            this.MountGSD(workingDirectory: null, enlistmentPath: SubstDrive);
        }

        [TestCase]
        public void GSDMountWorksFromJunction()
        {
            string enlistmentRootjunctionLink = Path.Combine(this.junctionsRoot, $"{nameof(this.GSDMountWorksFromJunction)}_ToEnlistmentRoot");
            this.CreateJunction(enlistmentRootjunctionLink, this.Enlistment.EnlistmentRoot);
            this.Enlistment.UnmountGSD();
            this.MountGSD(workingDirectory: enlistmentRootjunctionLink);

            string junctionLink = Path.Combine(this.junctionsRoot, $"{nameof(this.GSDMountWorksFromJunction)}_ToRepoRoot");
            this.CreateJunction(junctionLink, this.Enlistment.RepoRoot);
            this.Enlistment.UnmountGSD();
            this.MountGSD(workingDirectory: junctionLink);

            string subFolderPath = this.Enlistment.GetVirtualPathTo("GSD");
            subFolderPath.ShouldBeADirectory(this.fileSystem);
            junctionLink = Path.Combine(this.junctionsRoot, $"{nameof(this.GSDMountWorksFromJunction)}_ToSubFolder");
            this.CreateJunction(junctionLink, subFolderPath);
            this.Enlistment.UnmountGSD();
            this.MountGSD(workingDirectory: junctionLink);

            this.Enlistment.UnmountGSD();
            this.MountGSD(workingDirectory: null, enlistmentPath: enlistmentRootjunctionLink);
        }

        [TestCase]
        public void GitCommandInSubstToSubfolderWaitsWhileAnotherIsRunning()
        {
            this.CreateSubstDrive(this.Enlistment.EnlistmentRoot);
            this.GitCommandWaitsForLock(Path.Combine(SubstDrivePath, "src"));

            this.CreateSubstDrive(this.Enlistment.RepoRoot);
            this.GitCommandWaitsForLock(SubstDrivePath);
        }

        [TestCase]
        public void GitCommandInJunctionToSubfolderWaitsWhileAnotherIsRunning()
        {
            string junctionLink = Path.Combine(this.junctionsRoot, $"{nameof(this.GitCommandInJunctionToSubfolderWaitsWhileAnotherIsRunning)}_ToEnlistmentRoot");
            this.CreateJunction(junctionLink, this.Enlistment.EnlistmentRoot);
            this.GitCommandWaitsForLock(Path.Combine(junctionLink, "src"));

            junctionLink = Path.Combine(this.junctionsRoot, $"{nameof(this.GitCommandInJunctionToSubfolderWaitsWhileAnotherIsRunning)}_ToRepoRoot");
            this.CreateJunction(junctionLink, this.Enlistment.RepoRoot);
            this.GitCommandWaitsForLock(junctionLink);
        }

        private void GitCommandWaitsForLock(string gitWorkingDirectory)
        {
            ManualResetEventSlim resetEvent = GitHelpers.AcquireGSDLock(this.Enlistment, out _, resetTimeout: 3000);
            ProcessResult statusWait = GitHelpers.InvokeGitAgainstGSDRepo(gitWorkingDirectory, "status", removeWaitingMessages: false);
            statusWait.Errors.ShouldContain(ExpectedStatusWaitingText);
            resetEvent.Set();
            this.Enlistment.WaitForBackgroundOperations();
        }

        private void CreateSubstDrive(string path)
        {
            this.RemoveSubstDrive();
            this.fileSystem.DirectoryExists(path).ShouldBeTrue($"{path} needs to exist to be able to map it to a drive letter");
            string substResult = this.RunSubst($"{SubstDrive} {path}");
            this.fileSystem.DirectoryExists(SubstDrivePath).ShouldBeTrue($"{SubstDrivePath} should exist after creating mapping with subst. subst result: {substResult}");
        }

        private void RemoveSubstDrive()
        {
            string substResult = this.RunSubst($"{SubstDrive} /D");
            this.fileSystem.DirectoryExists(SubstDrivePath).ShouldBeFalse($"{SubstDrivePath} should not exist after being removed with subst /D. subst result: {substResult}");
        }

        private string RunSubst(string substArguments)
        {
            string cmdArguments = $"/C subst {substArguments}";
            ProcessResult result = ProcessHelper.Run("CMD.exe", cmdArguments);
            return !string.IsNullOrEmpty(result.Output) ? result.Output : result.Errors;
        }

        private void CreateJunction(string junctionLink, string junctionTarget)
        {
            junctionLink.ShouldNotExistOnDisk(this.fileSystem);
            junctionTarget.ShouldBeADirectory(this.fileSystem);
            ProcessHelper.Run("CMD.exe", "/C mklink /J " + junctionLink + " " + junctionTarget);
            junctionLink.ShouldBeADirectory(this.fileSystem);
        }

        private void RepoStatusShouldBeMounted(string workingDirectory, string enlistmentPath = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(GSDTestConfig.PathToGSD);
            startInfo.Arguments = "status" + (enlistmentPath != null ? $" {enlistmentPath}" : string.Empty);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = workingDirectory;

            ProcessResult result = ProcessHelper.Run(startInfo);
            result.ExitCode.ShouldEqual(0, result.Errors);
            result.Output.ShouldContain("Mount status: Ready");
        }

        private void MountGSD(string workingDirectory, string enlistmentPath = null)
        {
            string mountCommand;
            if (enlistmentPath != null)
            {
                mountCommand = $"mount \"{enlistmentPath}\" {TestConstants.InternalUseOnlyFlag} {GSDHelpers.GetInternalParameter()}";
            }
            else
            {
                mountCommand = $"mount {TestConstants.InternalUseOnlyFlag} {GSDHelpers.GetInternalParameter()}";
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(GSDTestConfig.PathToGSD);
            startInfo.Arguments = mountCommand;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = workingDirectory;

            ProcessResult result = ProcessHelper.Run(startInfo);
            result.ExitCode.ShouldEqual(0, result.Errors);

            this.RepoStatusShouldBeMounted(workingDirectory, enlistmentPath);
        }
    }
}
