using GSD.Common.Prefetch;
using GSD.Tests.Should;
using GSD.UnitTests.Mock.FileSystem;
using NUnit.Framework;
using System.IO;

namespace GSD.UnitTests.Prefetch
{
    [TestFixture]
    public class BlobPrefetcherTests
    {
        [TestCase]
        public void AppendToNewlineSeparatedFileTests()
        {
            MockFileSystem fileSystem = new MockFileSystem(new MockDirectory(Path.Combine("mock:", "GSD", "UnitTests", "Repo"), null, null));

            // Validate can write to a file that doesn't exist.
            string testFileName = Path.Combine("mock:", "GSD", "UnitTests", "Repo", "appendTests");
            BlobPrefetcher.AppendToNewlineSeparatedFile(fileSystem, testFileName, "expected content line 1");
            fileSystem.ReadAllText(testFileName).ShouldEqual("expected content line 1\n");

            // Validate that if the file doesn't end in a newline it gets a newline added.
            fileSystem.WriteAllText(testFileName, "existing content");
            BlobPrefetcher.AppendToNewlineSeparatedFile(fileSystem, testFileName, "expected line 2");
            fileSystem.ReadAllText(testFileName).ShouldEqual("existing content\nexpected line 2\n");

            // Validate that if the file ends in a newline, we don't end up with two newlines
            fileSystem.WriteAllText(testFileName, "existing content\n");
            BlobPrefetcher.AppendToNewlineSeparatedFile(fileSystem, testFileName, "expected line 2");
            fileSystem.ReadAllText(testFileName).ShouldEqual("existing content\nexpected line 2\n");
        }
    }
}