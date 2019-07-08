using GSD.Common;
using GSD.Common.Git;
using GSD.Common.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace GSD.UnitTests.Mock.Git
{
    public class MockGSDGitObjects : GSDGitObjects
    {
        private GSDContext context;

        public MockGSDGitObjects(GSDContext context, GitObjectsHttpRequestor httpGitObjects)
            : base(context, httpGitObjects)
        {
            this.context = context;
        }

        public bool CancelTryCopyBlobContentStream { get; set; }
        public uint FileLength { get; set; }

        public override bool TryDownloadCommit(string objectSha)
        {
            RetryWrapper<GitObjectsHttpRequestor.GitObjectTaskResult>.InvocationResult result = this.GitObjectRequestor.TryDownloadObjects(
                new[] { objectSha },
                onSuccess: (tryCount, response) =>
                {
                    // Add the contents to the mock repo
                    ((MockGitRepo)this.Context.Repository).AddBlob(objectSha, "DownloadedFile", response.RetryableReadToEnd());

                    return new RetryWrapper<GitObjectsHttpRequestor.GitObjectTaskResult>.CallbackResult(new GitObjectsHttpRequestor.GitObjectTaskResult(true));
                },
                onFailure: null,
                preferBatchedLooseObjects: false);

            return result.Succeeded && result.Result.Success;
        }

        public override bool TryCopyBlobContentStream(
            string sha,
            CancellationToken cancellationToken,
            RequestSource requestSource,
            Action<Stream, long> writeAction)
        {
            if (this.CancelTryCopyBlobContentStream)
            {
                throw new OperationCanceledException();
            }

            writeAction(
                new MemoryStream(new byte[this.FileLength]),
                this.FileLength);

            return true;
        }

        public override string[] ReadPackFileNames(string packFolderPath, string prefixFilter = "")
        {
            return Array.Empty<string>();
        }

        public override GitProcess.Result IndexPackFile(string packfilePath, GitProcess process)
        {
            return new GitProcess.Result("mocked", null, 0);
        }

        public override void DeleteStaleTempPrefetchPackAndIdxs()
        {
        }

        public override bool TryDownloadPrefetchPacks(GitProcess gitProcess, long latestTimestamp, out List<string> packIndexes)
        {
            packIndexes = new List<string>();
            return true;
        }
    }
}
