using GSD.Common;
using GSD.Common.Git;
using GSD.Common.Http;
using GSD.Tests.Should;
using GSD.UnitTests.Category;
using GSD.UnitTests.Mock;
using GSD.UnitTests.Mock.Common;
using GSD.UnitTests.Mock.FileSystem;
using GSD.UnitTests.Mock.Git;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;

namespace GSD.UnitTests.Git
{
    [TestFixture]
    public class GSDGitObjectsTests
    {
        private const string ValidTestObjectFileContents = "421dc4df5e1de427e363b8acd9ddb2d41385dbdf";
        private const string TestEnlistmentRoot = "mock:\\src";
        private const string TestLocalCacheRoot = "mock:\\.gvfs";
        private const string TestObjectRoot = "mock:\\.gvfs\\gitObjectCache";

        [TestCase]
        [Category(CategoryConstants.ExceptionExpected)]
        public void CatchesFileNotFoundAfterFileDeleted()
        {
            MockFileSystemWithCallbacks fileSystem = new MockFileSystemWithCallbacks();
            fileSystem.OnFileExists = () => true;
            fileSystem.OnOpenFileStream = (path, fileMode, fileAccess) =>
            {
                if (fileAccess == FileAccess.Write)
                {
                    return new MemoryStream();
                }

                throw new FileNotFoundException();
            };

            MockHttpGitObjects httpObjects = new MockHttpGitObjects();
            using (httpObjects.InputStream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(ValidTestObjectFileContents)))
            {
                httpObjects.MediaType = GSDConstants.MediaTypes.LooseObjectMediaType;
                GSDGitObjects dut = this.CreateTestableGSDGitObjects(httpObjects, fileSystem);

                dut.TryCopyBlobContentStream(
                    ValidTestObjectFileContents,
                    new CancellationToken(),
                    GSDGitObjects.RequestSource.FileStreamCallback,
                    (stream, length) => Assert.Fail("Should not be able to call copy stream callback"))
                    .ShouldEqual(false);
            }
        }

        [TestCase]
        public void SucceedsForNormalLookingLooseObjectDownloads()
        {
            MockFileSystemWithCallbacks fileSystem = new Mock.FileSystem.MockFileSystemWithCallbacks();
            fileSystem.OnFileExists = () => true;
            fileSystem.OnOpenFileStream = (path, mode, access) => new MemoryStream();
            MockHttpGitObjects httpObjects = new MockHttpGitObjects();
            using (httpObjects.InputStream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(ValidTestObjectFileContents)))
            {
                httpObjects.MediaType = GSDConstants.MediaTypes.LooseObjectMediaType;
                GSDGitObjects dut = this.CreateTestableGSDGitObjects(httpObjects, fileSystem);

                dut.TryDownloadAndSaveObject(ValidTestObjectFileContents, GSDGitObjects.RequestSource.FileStreamCallback)
                    .ShouldEqual(GitObjects.DownloadAndSaveObjectResult.Success);
            }
        }

        [TestCase]
        [Category(CategoryConstants.ExceptionExpected)]
        public void FailsZeroByteLooseObjectsDownloads()
        {
            this.AssertRetryableExceptionOnDownload(
                new MemoryStream(),
                GSDConstants.MediaTypes.LooseObjectMediaType,
                gitObjects => gitObjects.TryDownloadAndSaveObject("aabbcc", GSDGitObjects.RequestSource.FileStreamCallback));
        }

        [TestCase]
        [Category(CategoryConstants.ExceptionExpected)]
        public void FailsNullByteLooseObjectsDownloads()
        {
            this.AssertRetryableExceptionOnDownload(
                new MemoryStream(new byte[256]),
                GSDConstants.MediaTypes.LooseObjectMediaType,
                gitObjects => gitObjects.TryDownloadAndSaveObject("aabbcc", GSDGitObjects.RequestSource.FileStreamCallback));
        }

        [TestCase]
        [Category(CategoryConstants.ExceptionExpected)]
        public void FailsZeroBytePackDownloads()
        {
            this.AssertRetryableExceptionOnDownload(
                new MemoryStream(),
                GSDConstants.MediaTypes.PackFileMediaType,
                gitObjects => gitObjects.TryDownloadCommit("object0"));
        }

        [TestCase]
        [Category(CategoryConstants.ExceptionExpected)]
        public void FailsNullBytePackDownloads()
        {
            this.AssertRetryableExceptionOnDownload(
                new MemoryStream(new byte[256]),
                GSDConstants.MediaTypes.PackFileMediaType,
                gitObjects => gitObjects.TryDownloadCommit("object0"));
        }

        private void AssertRetryableExceptionOnDownload(
            MemoryStream inputStream,
            string mediaType,
            Action<GSDGitObjects> download)
        {
            MockHttpGitObjects httpObjects = new MockHttpGitObjects();
            httpObjects.InputStream = inputStream;
            httpObjects.MediaType = mediaType;
            MockFileSystemWithCallbacks fileSystem = new MockFileSystemWithCallbacks();

            using (ReusableMemoryStream downloadDestination = new ReusableMemoryStream(string.Empty))
            {
                fileSystem.OnFileExists = () => false;
                fileSystem.OnOpenFileStream = (path, mode, access) => downloadDestination;

                GSDGitObjects gitObjects = this.CreateTestableGSDGitObjects(httpObjects, fileSystem);

                Assert.Throws<RetryableException>(() => download(gitObjects));
                inputStream.Dispose();
            }
        }

        private GSDGitObjects CreateTestableGSDGitObjects(MockHttpGitObjects httpObjects, MockFileSystemWithCallbacks fileSystem)
        {
            MockTracer tracer = new MockTracer();
            GSDEnlistment enlistment = new GSDEnlistment(TestEnlistmentRoot, "https://fakeRepoUrl", "fakeGitBinPath", gvfsHooksRoot: null, authentication: null);
            enlistment.InitializeCachePathsFromKey(TestLocalCacheRoot, TestObjectRoot);
            GitRepo repo = new GitRepo(tracer, enlistment, fileSystem, () => new MockLibGit2Repo(tracer));

            GSDContext context = new GSDContext(tracer, fileSystem, repo, enlistment);
            GSDGitObjects dut = new GSDGitObjects(context, httpObjects);
            return dut;
        }

        private string GetDataPath(string fileName)
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(workingDirectory, "Data", fileName);
        }

        private class MockHttpGitObjects : GitObjectsHttpRequestor
        {
            public MockHttpGitObjects()
                : this(new MockGSDEnlistment())
            {
            }

            private MockHttpGitObjects(MockGSDEnlistment enlistment)
                : base(new MockTracer(), enlistment, new MockCacheServerInfo(), new RetryConfig(maxRetries: 1))
            {
            }

            public Stream InputStream { get; set; }
            public string MediaType { get; set; }

            public static MemoryStream GetRandomStream(int size)
            {
                Random randy = new Random(0);
                MemoryStream stream = new MemoryStream();
                byte[] buffer = new byte[size];

                randy.NextBytes(buffer);
                stream.Write(buffer, 0, buffer.Length);

                stream.Position = 0;
                return stream;
            }

            public override RetryWrapper<GitObjectTaskResult>.InvocationResult TryDownloadLooseObject(
                string objectId,
                bool retryOnFailure,
                CancellationToken cancellationToken,
                string requestSource,
                Func<int, GitEndPointResponseData, RetryWrapper<GitObjectTaskResult>.CallbackResult> onSuccess)
            {
                return this.TryDownloadObjects(new[] { objectId }, onSuccess, null, false);
            }

            public override RetryWrapper<GitObjectTaskResult>.InvocationResult TryDownloadObjects(
                IEnumerable<string> objectIds,
                Func<int, GitEndPointResponseData, RetryWrapper<GitObjectTaskResult>.CallbackResult> onSuccess,
                Action<RetryWrapper<GitObjectTaskResult>.ErrorEventArgs> onFailure,
                bool preferBatchedLooseObjects)
            {
                using (GitEndPointResponseData response = new GitEndPointResponseData(
                    HttpStatusCode.OK,
                    this.MediaType,
                    this.InputStream,
                    message: null,
                    onResponseDisposed: null))
                {
                    onSuccess(0, response);
                }

                GitObjectTaskResult result = new GitObjectTaskResult(true);
                return new RetryWrapper<GitObjectTaskResult>.InvocationResult(0, true, result);
            }

            public override List<GitObjectSize> QueryForFileSizes(IEnumerable<string> objectIds, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}