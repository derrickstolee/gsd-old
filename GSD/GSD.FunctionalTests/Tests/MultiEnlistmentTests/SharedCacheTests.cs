using GSD.FunctionalTests.FileSystemRunners;
using GSD.FunctionalTests.Should;
using GSD.FunctionalTests.Tools;
using GSD.Tests.Should;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GSD.FunctionalTests.Tests.MultiEnlistmentTests
{
    [TestFixture]
    [Category(Categories.ExtraCoverage)]
    public class SharedCacheTests : TestsWithMultiEnlistment
    {
        private const string WellKnownFile = "Readme.md";

        // This branch and commit sha should point to the same place.
        private const string WellKnownBranch = "FunctionalTests/20170602";
        private const string WellKnownCommitSha = "42eb6632beffae26893a3d6e1a9f48d652327c6f";

        private string localCachePath;
        private string localCacheParentPath;

        private FileSystemRunner fileSystem;

        public SharedCacheTests()
        {
            this.fileSystem = new SystemIORunner();
        }

        [SetUp]
        public void SetCacheLocation()
        {
            this.localCacheParentPath = Path.Combine(Properties.Settings.Default.EnlistmentRoot, "..", Guid.NewGuid().ToString("N"));
            this.localCachePath = Path.Combine(this.localCacheParentPath, ".customGSDCache");
        }

        [TestCase]
        public void SecondCloneDoesNotDownloadAdditionalObjects()
        {
            GSDFunctionalTestEnlistment enlistment1 = this.CloneAndMountEnlistment();
            File.ReadAllText(Path.Combine(enlistment1.RepoRoot, WellKnownFile));

            this.AlternatesFileShouldHaveGitObjectsRoot(enlistment1);

            string[] allObjects = Directory.EnumerateFiles(enlistment1.LocalCacheRoot, "*", SearchOption.AllDirectories).ToArray();

            GSDFunctionalTestEnlistment enlistment2 = this.CloneAndMountEnlistment();
            File.ReadAllText(Path.Combine(enlistment2.RepoRoot, WellKnownFile));

            this.AlternatesFileShouldHaveGitObjectsRoot(enlistment2);

            enlistment2.LocalCacheRoot.ShouldEqual(enlistment1.LocalCacheRoot, "Sanity: Local cache roots are expected to match.");
            Directory.EnumerateFiles(enlistment2.LocalCacheRoot, "*", SearchOption.AllDirectories)
                .ShouldMatchInOrder(allObjects);
        }

        [TestCase]
        public void RepairFixesCorruptBlobSizesDatabase()
        {
            GSDFunctionalTestEnlistment enlistment = this.CloneAndMountEnlistment();
            enlistment.UnmountGSD();

            // Repair on a healthy enlistment should succeed
            enlistment.Repair(confirm: true);

            string blobSizesRoot = GSDHelpers.GetPersistedBlobSizesRoot(enlistment.DotGSDRoot).ShouldNotBeNull();
            string blobSizesDbPath = Path.Combine(blobSizesRoot, "BlobSizes.sql");
            blobSizesDbPath.ShouldBeAFile(this.fileSystem);
            this.fileSystem.WriteAllText(blobSizesDbPath, "0000");

            enlistment.TryMountGSD().ShouldEqual(false, "GSD shouldn't mount when blob size db is corrupt");
            enlistment.Repair(confirm: true);
            enlistment.MountGSD();
        }

        [TestCase]
        [Category(Categories.MacTODO.NeedsServiceVerb)]
        public void CloneCleansUpStaleMetadataLock()
        {
            GSDFunctionalTestEnlistment enlistment1 = this.CloneAndMountEnlistment();
            string metadataLockPath = Path.Combine(this.localCachePath, "mapping.dat.lock");
            metadataLockPath.ShouldNotExistOnDisk(this.fileSystem);
            this.fileSystem.WriteAllText(metadataLockPath, enlistment1.EnlistmentRoot);
            metadataLockPath.ShouldBeAFile(this.fileSystem);

            GSDFunctionalTestEnlistment enlistment2 = this.CloneAndMountEnlistment();
            metadataLockPath.ShouldNotExistOnDisk(this.fileSystem);

            enlistment1.Status().ShouldContain("Mount status: Ready");
            enlistment2.Status().ShouldContain("Mount status: Ready");
        }

        [TestCase]
        public void ParallelReadsInASharedCache()
        {
            GSDFunctionalTestEnlistment enlistment1 = this.CloneAndMountEnlistment();
            GSDFunctionalTestEnlistment enlistment2 = this.CloneAndMountEnlistment();
            GSDFunctionalTestEnlistment enlistment3 = null;

            Task task1 = Task.Run(() => this.HydrateEntireRepo(enlistment1));
            Task task2 = Task.Run(() => this.HydrateEntireRepo(enlistment2));
            Task task3 = Task.Run(() => enlistment3 = this.CloneAndMountEnlistment());

            task1.Wait();
            task2.Wait();
            task3.Wait();

            task1.Exception.ShouldBeNull();
            task2.Exception.ShouldBeNull();
            task3.Exception.ShouldBeNull();

            enlistment1.Status().ShouldContain("Mount status: Ready");
            enlistment2.Status().ShouldContain("Mount status: Ready");
            enlistment3.Status().ShouldContain("Mount status: Ready");

            this.AlternatesFileShouldHaveGitObjectsRoot(enlistment1);
            this.AlternatesFileShouldHaveGitObjectsRoot(enlistment2);
            this.AlternatesFileShouldHaveGitObjectsRoot(enlistment3);
        }

        [TestCase]
        public void DeleteObjectsCacheAndCacheMappingBeforeMount()
        {
            GSDFunctionalTestEnlistment enlistment1 = this.CloneAndMountEnlistment();
            GSDFunctionalTestEnlistment enlistment2 = this.CloneAndMountEnlistment();

            enlistment1.UnmountGSD();

            string objectsRoot = GSDHelpers.GetPersistedGitObjectsRoot(enlistment1.DotGSDRoot).ShouldNotBeNull();
            objectsRoot.ShouldBeADirectory(this.fileSystem);
            RepositoryHelpers.DeleteTestDirectory(objectsRoot);

            string metadataPath = Path.Combine(this.localCachePath, "mapping.dat");
            metadataPath.ShouldBeAFile(this.fileSystem);
            this.fileSystem.DeleteFile(metadataPath);

            enlistment1.MountGSD();

            Task task1 = Task.Run(() => this.HydrateRootFolder(enlistment1));
            Task task2 = Task.Run(() => this.HydrateRootFolder(enlistment2));
            task1.Wait();
            task2.Wait();
            task1.Exception.ShouldBeNull();
            task2.Exception.ShouldBeNull();

            enlistment1.Status().ShouldContain("Mount status: Ready");
            enlistment2.Status().ShouldContain("Mount status: Ready");

            this.AlternatesFileShouldHaveGitObjectsRoot(enlistment1);
            this.AlternatesFileShouldHaveGitObjectsRoot(enlistment2);
        }

        [TestCase]
        public void DeleteCacheDuringHydrations()
        {
            GSDFunctionalTestEnlistment enlistment1 = this.CloneAndMountEnlistment();

            string objectsRoot = GSDHelpers.GetPersistedGitObjectsRoot(enlistment1.DotGSDRoot).ShouldNotBeNull();
            objectsRoot.ShouldBeADirectory(this.fileSystem);

            Task task1 = Task.Run(() =>
            {
                this.HydrateEntireRepo(enlistment1);
            });

            while (!task1.IsCompleted)
            {
                try
                {
                    // Delete objectsRoot rather than this.localCachePath as the blob sizes database cannot be deleted while GSD is mounted
                    RepositoryHelpers.DeleteTestDirectory(objectsRoot);
                    Thread.Sleep(100);
                }
                catch (IOException)
                {
                    // Hydration may have handles into the cache, so failing this delete is expected.
                }
            }

            task1.Exception.ShouldBeNull();

            enlistment1.Status().ShouldContain("Mount status: Ready");
        }

        [TestCase]
        public void DownloadingACommitWithoutTreesDoesntBreakNextClone()
        {
            GSDFunctionalTestEnlistment enlistment1 = this.CloneAndMountEnlistment();
            GitProcess.Invoke(enlistment1.RepoRoot, "cat-file -s " + WellKnownCommitSha).ShouldEqual("293\n");

            GSDFunctionalTestEnlistment enlistment2 = this.CloneAndMountEnlistment(WellKnownBranch);
            enlistment2.Status().ShouldContain("Mount status: Ready");
        }

        [TestCase]
        public void MountReusesLocalCacheKeyWhenGitObjectsRootDeleted()
        {
            GSDFunctionalTestEnlistment enlistment = this.CloneAndMountEnlistment();

            enlistment.UnmountGSD();

            // Find the current git objects root and ensure it's on disk
            string objectsRoot = GSDHelpers.GetPersistedGitObjectsRoot(enlistment.DotGSDRoot).ShouldNotBeNull();
            objectsRoot.ShouldBeADirectory(this.fileSystem);

            string mappingFilePath = Path.Combine(enlistment.LocalCacheRoot, "mapping.dat");
            string mappingFileContents = this.fileSystem.ReadAllText(mappingFilePath);
            mappingFileContents.Length.ShouldNotEqual(0, "mapping.dat should not be empty");

            // Delete the git objects root folder, mount should re-create it and the mapping.dat file should not change
            RepositoryHelpers.DeleteTestDirectory(objectsRoot);

            enlistment.MountGSD();

            GSDHelpers.GetPersistedGitObjectsRoot(enlistment.DotGSDRoot).ShouldEqual(objectsRoot);
            objectsRoot.ShouldBeADirectory(this.fileSystem);
            mappingFilePath.ShouldBeAFile(this.fileSystem).WithContents(mappingFileContents);

            this.AlternatesFileShouldHaveGitObjectsRoot(enlistment);
        }

        [TestCase]
        public void MountUsesNewLocalCacheKeyWhenLocalCacheDeleted()
        {
            GSDFunctionalTestEnlistment enlistment = this.CloneAndMountEnlistment();

            enlistment.UnmountGSD();

            // Find the current git objects root and ensure it's on disk
            string objectsRoot = GSDHelpers.GetPersistedGitObjectsRoot(enlistment.DotGSDRoot).ShouldNotBeNull();
            objectsRoot.ShouldBeADirectory(this.fileSystem);

            string mappingFilePath = Path.Combine(enlistment.LocalCacheRoot, "mapping.dat");
            string mappingFileContents = this.fileSystem.ReadAllText(mappingFilePath);
            mappingFileContents.Length.ShouldNotEqual(0, "mapping.dat should not be empty");

            // Delete the local cache folder, mount should re-create it and generate a new mapping file and local cache key
            RepositoryHelpers.DeleteTestDirectory(enlistment.LocalCacheRoot);

            enlistment.MountGSD();

            // Mount should recreate the local cache root
            enlistment.LocalCacheRoot.ShouldBeADirectory(this.fileSystem);

            // Determine the new local cache key
            string newMappingFileContents = mappingFilePath.ShouldBeAFile(this.fileSystem).WithContents();
            const int GuidStringLength = 32;
            string mappingFileKey = "A {\"Key\":\"https://gvfs.visualstudio.com/ci/_git/fortests\",\"Value\":\"";
            int localKeyIndex = newMappingFileContents.IndexOf(mappingFileKey);
            string newCacheKey = newMappingFileContents.Substring(localKeyIndex + mappingFileKey.Length, GuidStringLength);

            // Validate the new objects root is on disk and uses the new key
            objectsRoot.ShouldNotExistOnDisk(this.fileSystem);
            string newObjectsRoot = GSDHelpers.GetPersistedGitObjectsRoot(enlistment.DotGSDRoot);
            newObjectsRoot.ShouldNotEqual(objectsRoot);
            newObjectsRoot.ShouldContain(newCacheKey);
            newObjectsRoot.ShouldBeADirectory(this.fileSystem);

            this.AlternatesFileShouldHaveGitObjectsRoot(enlistment);
        }

        [TestCase]
        public void SecondCloneSucceedsWithMissingTrees()
        {
            string newCachePath = Path.Combine(this.localCacheParentPath, ".customGvfsCache2");
            GSDFunctionalTestEnlistment enlistment1 = this.CreateNewEnlistment(localCacheRoot: newCachePath, skipPrefetch: true);
            File.ReadAllText(Path.Combine(enlistment1.RepoRoot, WellKnownFile));
            this.AlternatesFileShouldHaveGitObjectsRoot(enlistment1);

            // This Git command loads the commit and root tree for WellKnownCommitSha,
            // but does not download any more reachable objects.
            string command = "cat-file -p origin/" + WellKnownBranch + "^{tree}";
            ProcessResult result = GitHelpers.InvokeGitAgainstGSDRepo(enlistment1.RepoRoot, command);
            result.ExitCode.ShouldEqual(0, $"git {command} failed with error: " + result.Errors);

            // If we did not properly check the failed checkout at this step, then clone will fail during checkout.
            GSDFunctionalTestEnlistment enlistment2 = this.CreateNewEnlistment(localCacheRoot: newCachePath, branch: WellKnownBranch, skipPrefetch: true);
            File.ReadAllText(Path.Combine(enlistment2.RepoRoot, WellKnownFile));
        }

        // Override OnTearDownEnlistmentsDeleted rathern than using [TearDown] as the enlistments need to be unmounted before
        // localCacheParentPath can be deleted (as the SQLite blob sizes database cannot be deleted while GSD is mounted)
        protected override void OnTearDownEnlistmentsDeleted()
        {
            RepositoryHelpers.DeleteTestDirectory(this.localCacheParentPath);
        }

        private GSDFunctionalTestEnlistment CloneAndMountEnlistment(string branch = null)
        {
            return this.CreateNewEnlistment(this.localCachePath, branch);
        }

        private void AlternatesFileShouldHaveGitObjectsRoot(GSDFunctionalTestEnlistment enlistment)
        {
            string objectsRoot = GSDHelpers.GetPersistedGitObjectsRoot(enlistment.DotGSDRoot);
            string alternatesFileContents = Path.Combine(enlistment.RepoRoot, ".git", "objects", "info", "alternates").ShouldBeAFile(this.fileSystem).WithContents();
            alternatesFileContents.ShouldEqual(objectsRoot);
        }

        private void HydrateRootFolder(GSDFunctionalTestEnlistment enlistment)
        {
            List<string> allFiles = Directory.EnumerateFiles(enlistment.RepoRoot, "*", SearchOption.TopDirectoryOnly).ToList();
            for (int i = 0; i < allFiles.Count; ++i)
            {
                File.ReadAllText(allFiles[i]);
            }
        }

        private void HydrateEntireRepo(GSDFunctionalTestEnlistment enlistment)
        {
            List<string> allFiles = Directory.EnumerateFiles(enlistment.RepoRoot, "*", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < allFiles.Count; ++i)
            {
                if (!allFiles[i].StartsWith(enlistment.RepoRoot + "\\.git\\", StringComparison.OrdinalIgnoreCase))
                {
                    File.ReadAllText(allFiles[i]);
                }
            }
        }
    }
}
