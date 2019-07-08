using GSD.Common;
using GSD.Common.FileSystem;
using GSD.Tests.Should;
using GSD.UnitTests.Category;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GSD.UnitTests.CommandLine
{
    [TestFixture]
    public class HooksInstallerTests
    {
        private const string Filename = "hooksfile";

        [TestCase]
        [Category(CategoryConstants.ExceptionExpected)]
        public void MergeHooksDataThrowsOnFoundGSDHooks()
        {
            Assert.Throws<HooksInstaller.HooksConfigurationException>(
                () => HooksInstaller.MergeHooksData(
                    new string[] { "first", GSDPlatform.Instance.Constants.GSDHooksExecutableName },
                    Filename,
                    GSDConstants.DotGit.Hooks.PreCommandHookName));
        }

        [TestCase]
        public void MergeHooksDataEmptyConfig()
        {
            string result = HooksInstaller.MergeHooksData(new string[] { }, Filename, GSDConstants.DotGit.Hooks.PreCommandHookName);
            IEnumerable<string> resultLines = result
                .Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.StartsWith("#"));

            resultLines.Single().ShouldEqual(GSDPlatform.Instance.Constants.GSDHooksExecutableName);
        }

        [TestCase]
        public void MergeHooksDataPreCommandLast()
        {
            string result = HooksInstaller.MergeHooksData(new string[] { "first", "second" }, Filename, GSDConstants.DotGit.Hooks.PreCommandHookName);
            IEnumerable<string> resultLines = result
                .Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.StartsWith("#"));

            resultLines.Count().ShouldEqual(3);
            resultLines.ElementAt(0).ShouldEqual("first");
            resultLines.ElementAt(1).ShouldEqual("second");
            resultLines.ElementAt(2).ShouldEqual(GSDPlatform.Instance.Constants.GSDHooksExecutableName);
        }

        [TestCase]
        public void MergeHooksDataPostCommandFirst()
        {
            string result = HooksInstaller.MergeHooksData(new string[] { "first", "second" }, Filename, GSDConstants.DotGit.Hooks.PostCommandHookName);
            IEnumerable<string> resultLines = result
                .Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.StartsWith("#"));

            resultLines.Count().ShouldEqual(3);
            resultLines.ElementAt(0).ShouldEqual(GSDPlatform.Instance.Constants.GSDHooksExecutableName);
            resultLines.ElementAt(1).ShouldEqual("first");
            resultLines.ElementAt(2).ShouldEqual("second");
        }

        [TestCase]
        public void MergeHooksDataDiscardBlankLines()
        {
            string result = HooksInstaller.MergeHooksData(new string[] { "first", "second", string.Empty, " " }, Filename, GSDConstants.DotGit.Hooks.PreCommandHookName);
            IEnumerable<string> resultLines = result
                .Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.StartsWith("#"));

            resultLines.Count().ShouldEqual(3);
            resultLines.ElementAt(0).ShouldEqual("first");
            resultLines.ElementAt(1).ShouldEqual("second");
            resultLines.ElementAt(2).ShouldEqual(GSDPlatform.Instance.Constants.GSDHooksExecutableName);
        }
    }
}
