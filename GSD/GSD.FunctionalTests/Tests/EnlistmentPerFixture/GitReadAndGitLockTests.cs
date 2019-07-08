using GSD.FunctionalTests.FileSystemRunners;
using GSD.FunctionalTests.Should;
using GSD.FunctionalTests.Tools;
using GSD.Tests.Should;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GSD.FunctionalTests.Tests.EnlistmentPerFixture
{
    [TestFixture]
    public class GitReadAndGitLockTests : TestsWithEnlistmentPerFixture
    {
        private const string ExpectedStatusWaitingText = @"Waiting for 'GSD.FunctionalTests.LockHolder'";
        private FileSystemRunner fileSystem;

        public GitReadAndGitLockTests()
        {
            this.fileSystem = new SystemIORunner();
        }

        [TestCase, Order(1)]
        public void GitStatus()
        {
            GitHelpers.CheckGitCommandAgainstGSDRepo(
                this.Enlistment.RepoRoot,
                "status",
                "On branch " + Properties.Settings.Default.Commitish,
                "nothing to commit, working tree clean");
        }

        [TestCase, Order(2)]
        public void GitLog()
        {
            GitHelpers.CheckGitCommandAgainstGSDRepo(this.Enlistment.RepoRoot, "log -n1", "commit", "Author:", "Date:");
        }

        [TestCase, Order(3)]
        public void GitBranch()
        {
            GitHelpers.CheckGitCommandAgainstGSDRepo(
                this.Enlistment.RepoRoot,
                "branch -a",
                "* " + Properties.Settings.Default.Commitish,
                "remotes/origin/" + Properties.Settings.Default.Commitish);
        }

        [TestCase, Order(4)]
        public void GitCommandWaitsWhileAnotherIsRunning()
        {
            int pid;
            GitHelpers.AcquireGSDLock(this.Enlistment, out pid, resetTimeout: 3000);

            ProcessResult statusWait = GitHelpers.InvokeGitAgainstGSDRepo(this.Enlistment.RepoRoot, "status", removeWaitingMessages: false);
            statusWait.Errors.ShouldContain(ExpectedStatusWaitingText);
        }

        [TestCase, Order(5)]
        public void GitAliasNamedAfterKnownCommandAcquiresLock()
        {
            string alias = nameof(this.GitAliasNamedAfterKnownCommandAcquiresLock);

            int pid;
            GitHelpers.AcquireGSDLock(this.Enlistment, out pid, resetTimeout: 3000);
            GitHelpers.CheckGitCommandAgainstGSDRepo(this.Enlistment.RepoRoot, "config --local alias." + alias + " status");
            ProcessResult statusWait = GitHelpers.InvokeGitAgainstGSDRepo(this.Enlistment.RepoRoot, alias, removeWaitingMessages: false);
            statusWait.Errors.ShouldContain(ExpectedStatusWaitingText);
        }

        [TestCase, Order(6)]
        public void GitAliasInSubfolderNamedAfterKnownCommandAcquiresLock()
        {
            string alias = nameof(this.GitAliasInSubfolderNamedAfterKnownCommandAcquiresLock);

            int pid;
            GitHelpers.AcquireGSDLock(this.Enlistment, out pid, resetTimeout: 3000);
            GitHelpers.CheckGitCommandAgainstGSDRepo(this.Enlistment.RepoRoot, "config --local alias." + alias + " rebase");
            ProcessResult statusWait = GitHelpers.InvokeGitAgainstGSDRepo(
                Path.Combine(this.Enlistment.RepoRoot, "GSD"),
                alias + " origin/FunctionalTests/RebaseTestsSource_20170208",
                removeWaitingMessages: false);
            statusWait.Errors.ShouldContain(ExpectedStatusWaitingText);
            GitHelpers.CheckGitCommandAgainstGSDRepo(this.Enlistment.RepoRoot, "rebase --abort");
        }

        [TestCase, Order(7)]
        public void ExternalLockHolderReportedWhenBackgroundTasksArePending()
        {
            int pid;
            GitHelpers.AcquireGSDLock(this.Enlistment, out pid, resetTimeout: 3000);

            // Creating a new file will queue a background task
            string newFilePath = this.Enlistment.GetVirtualPathTo("ExternalLockHolderReportedWhenBackgroundTasksArePending.txt");
            newFilePath.ShouldNotExistOnDisk(this.fileSystem);
            this.fileSystem.WriteAllText(newFilePath, "New file contents");

            ProcessResult statusWait = GitHelpers.InvokeGitAgainstGSDRepo(this.Enlistment.RepoRoot, "status", removeWaitingMessages: false);

            // Validate that GSD still reports that the git command is holding the lock
            statusWait.Errors.ShouldContain(ExpectedStatusWaitingText);
        }

        [TestCase, Order(8)]
        public void OrphanedGSDLockIsCleanedUp()
        {
            int pid;
            GitHelpers.AcquireGSDLock(this.Enlistment, out pid, resetTimeout: 1000, skipReleaseLock: true);

            while (true)
            {
                try
                {
                    using (Process.GetProcessById(pid))
                    {
                    }

                    Thread.Sleep(1000);
                }
                catch (ArgumentException)
                {
                    break;
                }
            }

            ProcessResult statusWait = GitHelpers.InvokeGitAgainstGSDRepo(this.Enlistment.RepoRoot, "status", removeWaitingMessages: false);

            // There should not be any errors - in particular, there should not be
            // an error about "Waiting for GSD.FunctionalTests.LockHolder"
            statusWait.Errors.ShouldEqual(string.Empty);
        }
    }
}
