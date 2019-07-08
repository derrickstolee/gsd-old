using GSD.FunctionalTests.FileSystemRunners;
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
    public class CloneTests : TestsWithEnlistmentPerFixture
    {
        private const int GSDGenericError = 3;

        [TestCase]
        public void CloneInsideMountedEnlistment()
        {
            this.SubfolderCloneShouldFail();
        }

        [TestCase]
        public void CloneInsideUnmountedEnlistment()
        {
            this.Enlistment.UnmountGSD();
            this.SubfolderCloneShouldFail();
            this.Enlistment.MountGSD();
        }

        [TestCase]
        public void CloneWithLocalCachePathWithinSrc()
        {
            string newEnlistmentRoot = GSDFunctionalTestEnlistment.GetUniqueEnlistmentRoot();

            ProcessStartInfo processInfo = new ProcessStartInfo(GSDTestConfig.PathToGSD);
            processInfo.Arguments = $"clone {Properties.Settings.Default.RepoToClone} {newEnlistmentRoot} --local-cache-path {Path.Combine(newEnlistmentRoot, "src", ".gvfsCache")}";
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.CreateNoWindow = true;
            processInfo.WorkingDirectory = Path.GetDirectoryName(this.Enlistment.EnlistmentRoot);
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;

            ProcessResult result = ProcessHelper.Run(processInfo);
            result.ExitCode.ShouldEqual(GSDGenericError);
            result.Output.ShouldContain("'--local-cache-path' cannot be inside the src folder");
        }

        [TestCase]
        [Category(Categories.MacOnly)]
        public void CloneWithDefaultLocalCacheLocation()
        {
            FileSystemRunner fileSystem = FileSystemRunner.DefaultRunner;
            string homeDirectory = Environment.GetEnvironmentVariable("HOME");
            homeDirectory.ShouldBeADirectory(fileSystem);

            string newEnlistmentRoot = GSDFunctionalTestEnlistment.GetUniqueEnlistmentRoot();

            ProcessStartInfo processInfo = new ProcessStartInfo(GSDTestConfig.PathToGSD);
            processInfo.Arguments = $"clone {Properties.Settings.Default.RepoToClone} {newEnlistmentRoot} --no-mount --no-prefetch";
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;

            ProcessResult result = ProcessHelper.Run(processInfo);
            result.ExitCode.ShouldEqual(0, result.Errors);

            string dotGSDRoot = Path.Combine(newEnlistmentRoot, GSDTestConfig.DotGSDRoot);
            dotGSDRoot.ShouldBeADirectory(fileSystem);
            string localCacheRoot = GSDHelpers.GetPersistedLocalCacheRoot(dotGSDRoot);
            string gitObjectsRoot = GSDHelpers.GetPersistedGitObjectsRoot(dotGSDRoot);

            string defaultGSDCacheRoot = Path.Combine(homeDirectory, ".gvfsCache");
            localCacheRoot.StartsWith(defaultGSDCacheRoot, StringComparison.Ordinal).ShouldBeTrue($"Local cache root did not default to using {homeDirectory}");
            gitObjectsRoot.StartsWith(defaultGSDCacheRoot, StringComparison.Ordinal).ShouldBeTrue($"Git objects root did not default to using {homeDirectory}");

            RepositoryHelpers.DeleteTestDirectory(newEnlistmentRoot);
        }

        [TestCase]
        public void CloneToPathWithSpaces()
        {
            GSDFunctionalTestEnlistment enlistment = GSDFunctionalTestEnlistment.CloneAndMountEnlistmentWithSpacesInPath(GSDTestConfig.PathToGSD);
            enlistment.UnmountAndDeleteAll();
        }

        private void SubfolderCloneShouldFail()
        {
            ProcessStartInfo processInfo = new ProcessStartInfo(GSDTestConfig.PathToGSD);
            processInfo.Arguments = "clone " + GSDTestConfig.RepoToClone + " src\\gvfs\\test1";
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.CreateNoWindow = true;
            processInfo.WorkingDirectory = this.Enlistment.EnlistmentRoot;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;

            ProcessResult result = ProcessHelper.Run(processInfo);
            result.ExitCode.ShouldEqual(GSDGenericError);
            result.Output.ShouldContain("You can't clone inside an existing GSD repo");
        }
    }
}
