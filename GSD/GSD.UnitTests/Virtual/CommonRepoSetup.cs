﻿using GSD.Common;
using GSD.Common.Git;
using GSD.UnitTests.Mock.Common;
using GSD.UnitTests.Mock.FileSystem;
using GSD.UnitTests.Mock.Git;
using System;
using System.IO;

namespace GSD.UnitTests.Virtual
{
    public class CommonRepoSetup : IDisposable
    {
        public CommonRepoSetup()
        {
            MockTracer tracer = new MockTracer();

            string enlistmentRoot = Path.Combine("mock:", "GSD", "UnitTests", "Repo");
            GSDEnlistment enlistment = new GSDEnlistment(enlistmentRoot, "fake://repoUrl", "fake://gitBinPath", gvfsHooksRoot: null, authentication: null);
            enlistment.InitializeCachePathsFromKey("fake:\\gvfsSharedCache", "fakeCacheKey");

            this.GitParentPath = enlistment.WorkingDirectoryRoot;
            this.GSDMetadataPath = enlistment.DotGSDRoot;

            MockDirectory enlistmentDirectory = new MockDirectory(
                enlistmentRoot,
                new MockDirectory[]
                {
                    new MockDirectory(this.GitParentPath, folders: null, files: null),
                },
                null);
            enlistmentDirectory.CreateFile(Path.Combine(this.GitParentPath, ".git", "config"), ".git config Contents", createDirectories: true);
            enlistmentDirectory.CreateFile(Path.Combine(this.GitParentPath, ".git", "HEAD"), ".git HEAD Contents", createDirectories: true);
            enlistmentDirectory.CreateFile(Path.Combine(this.GitParentPath, ".git", "logs", "HEAD"), "HEAD Contents", createDirectories: true);
            enlistmentDirectory.CreateFile(Path.Combine(this.GitParentPath, ".git", "info", "always_exclude"), "always_exclude Contents", createDirectories: true);
            enlistmentDirectory.CreateDirectory(enlistment.GitPackRoot);

            this.FileSystem = new MockFileSystem(enlistmentDirectory);
            this.Repository = new MockGitRepo(
                tracer,
                enlistment,
                this.FileSystem);
            CreateStandardGitTree(this.Repository);

            this.Context = new GSDContext(tracer, this.FileSystem, this.Repository, enlistment);

            this.HttpObjects = new MockHttpGitObjects(tracer, enlistment);
            this.GitObjects = new MockGSDGitObjects(this.Context, this.HttpObjects);
        }

        public GSDContext Context { get; private set; }

        public string GitParentPath { get; private set; }

        public string GSDMetadataPath { get; private set; }
        public GSDGitObjects GitObjects { get; private set; }

        public MockGitRepo Repository { get; private set; }
        public MockHttpGitObjects HttpObjects { get; private set; }
        public MockFileSystem FileSystem { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.Context != null)
                {
                    this.Context.Dispose();
                    this.Context = null;
                }

                if (this.HttpObjects != null)
                {
                    this.HttpObjects.Dispose();
                    this.HttpObjects = null;
                }
            }
        }

        private static void CreateStandardGitTree(MockGitRepo repository)
        {
            string rootSha = repository.GetHeadTreeSha();

            string atreeSha = repository.AddChildTree(rootSha, "A");
            repository.AddChildBlob(atreeSha, "A.1.txt", "A.1 in GitTree");
            repository.AddChildBlob(atreeSha, "A.2.txt", "A.2 in GitTree");

            string btreeSha = repository.AddChildTree(rootSha, "B");
            repository.AddChildBlob(btreeSha, "B.1.txt", "B.1 in GitTree");

            string dupContentSha = repository.AddChildTree(rootSha, "DupContent");
            repository.AddChildBlob(dupContentSha, "dup1.txt", "This is some duplicate content");
            repository.AddChildBlob(dupContentSha, "dup2.txt", "This is some duplicate content");

            string dupTreeSha = repository.AddChildTree(rootSha, "DupTree");
            repository.AddChildBlob(dupTreeSha, "B.1.txt", "B.1 in GitTree");

            repository.AddChildBlob(rootSha, "C.txt", "C in GitTree");
        }
    }
}