using GSD.FunctionalTests.Tools;
using GSD.Tests.Should;
using NUnit.Framework;

namespace GSD.FunctionalTests.Tests.GitCommands
{
    [TestFixtureSource(typeof(GitRepoTests), nameof(GitRepoTests.ValidateWorkingTree))]
    [Category(Categories.GitCommands)]
    public class MergeConflictTests : GitRepoTests
    {
        public MergeConflictTests(bool validateWorkingTree)
            : base(enlistmentPerTest: true, validateWorkingTree: validateWorkingTree)
        {
        }

        [TestCase]
        public void MergeConflict()
        {
            // No need to tear down this config since these tests are for enlistment per test.
            this.SetupRenameDetectionAvoidanceInConfig();

            this.ValidateGitCommand("checkout " + GitRepoTests.ConflictTargetBranch);
            this.RunGitCommand("merge " + GitRepoTests.ConflictSourceBranch);
            this.FilesShouldMatchAfterConflict();
        }

        [TestCase]
        public void MergeConflictWithFileReads()
        {
            // No need to tear down this config since these tests are for enlistment per test.
            this.SetupRenameDetectionAvoidanceInConfig();

            this.ValidateGitCommand("checkout " + GitRepoTests.ConflictTargetBranch);
            this.ReadConflictTargetFiles();
            this.RunGitCommand("merge " + GitRepoTests.ConflictSourceBranch);
            this.FilesShouldMatchAfterConflict();
        }

        [TestCase]
        public void MergeConflict_ThenAbort()
        {
            // No need to tear down this config since these tests are for enlistment per test.
            this.SetupRenameDetectionAvoidanceInConfig();

            this.ValidateGitCommand("checkout " + GitRepoTests.ConflictTargetBranch);
            this.RunGitCommand("merge " + GitRepoTests.ConflictSourceBranch);
            this.ValidateGitCommand("merge --abort");
            this.FilesShouldMatchCheckoutOfTargetBranch();
        }

        [TestCase]
        public void MergeConflict_UsingOurs()
        {
            // No need to tear down this config since these tests are for enlistment per test.
            this.SetupRenameDetectionAvoidanceInConfig();

            this.ValidateGitCommand("checkout " + GitRepoTests.ConflictTargetBranch);
            this.RunGitCommand($"merge -s ours {GitRepoTests.ConflictSourceBranch}");
            this.FilesShouldMatchCheckoutOfTargetBranch();
        }

        [TestCase]
        public void MergeConflict_UsingStrategyTheirs()
        {
            // No need to tear down this config since these tests are for enlistment per test.
            this.SetupRenameDetectionAvoidanceInConfig();

            this.ValidateGitCommand("checkout " + GitRepoTests.ConflictTargetBranch);
            this.RunGitCommand($"merge -s recursive -Xtheirs {GitRepoTests.ConflictSourceBranch}");
            this.FilesShouldMatchAfterConflict();
        }

        [TestCase]
        public void MergeConflict_UsingStrategyOurs()
        {
            // No need to tear down this config since these tests are for enlistment per test.
            this.SetupRenameDetectionAvoidanceInConfig();

            this.ValidateGitCommand("checkout " + GitRepoTests.ConflictTargetBranch);
            this.RunGitCommand($"merge -s recursive -Xours {GitRepoTests.ConflictSourceBranch}");
            this.FilesShouldMatchAfterConflict();
        }

        [TestCase]
        public void MergeConflictEnsureStatusFailsDueToConfig()
        {
            // This is compared against the message emitted by GSD.Hooks\Program.cs
            string expectedErrorMessagePart = "--no-renames";

            this.ValidateGitCommand("checkout " + GitRepoTests.ConflictTargetBranch);
            this.RunGitCommand("merge " + GitRepoTests.ConflictSourceBranch, checkStatus: false);

            ProcessResult result1 = GitHelpers.InvokeGitAgainstGSDRepo(this.Enlistment.RepoRoot, "status");
            result1.Errors.Contains(expectedErrorMessagePart);

            ProcessResult result2 = GitHelpers.InvokeGitAgainstGSDRepo(this.Enlistment.RepoRoot, "status --no-renames");
            result2.Errors.Contains(expectedErrorMessagePart);

            // Complete setup to ensure teardown succeeds
            GitHelpers.InvokeGitAgainstGSDRepo(this.Enlistment.RepoRoot, "config --local test.renames false");
        }

        protected override void CreateEnlistment()
        {
            base.CreateEnlistment();
            this.ControlGitRepo.Fetch(GitRepoTests.ConflictSourceBranch);
            this.ControlGitRepo.Fetch(GitRepoTests.ConflictTargetBranch);
            this.ValidateGitCommand("checkout " + GitRepoTests.ConflictSourceBranch);
            this.ValidateGitCommand("checkout " + GitRepoTests.ConflictTargetBranch);
        }

        private void SetupRenameDetectionAvoidanceInConfig()
        {
            // Tell the pre-command hook that it shouldn't check for "--no-renames" when runing "git status"
            // as the control repo won't do that.  When the pre-command hook has been updated to properly
            // check for "status.renames" we can set that value here instead.
            this.ValidateGitCommand("config --local test.renames false");
        }
    }
}
