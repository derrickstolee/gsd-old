﻿using GSD.FunctionalTests.FileSystemRunners;
using GSD.FunctionalTests.Should;
using GSD.Tests.Should;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace GSD.FunctionalTests.Tools
{
    public static class GSDHelpers
    {
        public static readonly string RepoMetadataName = Path.Combine("databases", "RepoMetadata.dat");

        private const string DiskLayoutMajorVersionKey = "DiskLayoutVersion";
        private const string DiskLayoutMinorVersionKey = "DiskLayoutMinorVersion";
        private const string LocalCacheRootKey = "LocalCacheRoot";
        private const string GitObjectsRootKey = "GitObjectsRoot";
        private const string BlobSizesRootKey = "BlobSizesRoot";

        public static string ConvertPathToGitFormat(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, TestConstants.GitPathSeparator);
        }

        public static void SaveDiskLayoutVersion(string dotGSDRoot, string majorVersion, string minorVersion)
        {
            SavePersistedValue(dotGSDRoot, DiskLayoutMajorVersionKey, majorVersion);
            SavePersistedValue(dotGSDRoot, DiskLayoutMinorVersionKey, minorVersion);
        }

        public static void GetPersistedDiskLayoutVersion(string dotGSDRoot, out string majorVersion, out string minorVersion)
        {
            majorVersion = GetPersistedValue(dotGSDRoot, DiskLayoutMajorVersionKey);
            minorVersion = GetPersistedValue(dotGSDRoot, DiskLayoutMinorVersionKey);
        }

        public static void SaveLocalCacheRoot(string dotGSDRoot, string value)
        {
            SavePersistedValue(dotGSDRoot, LocalCacheRootKey, value);
        }

        public static string GetPersistedLocalCacheRoot(string dotGSDRoot)
        {
            return GetPersistedValue(dotGSDRoot, LocalCacheRootKey);
        }

        public static void SaveGitObjectsRoot(string dotGSDRoot, string value)
        {
            SavePersistedValue(dotGSDRoot, GitObjectsRootKey, value);
        }

        public static string GetPersistedGitObjectsRoot(string dotGSDRoot)
        {
            return GetPersistedValue(dotGSDRoot, GitObjectsRootKey);
        }

        public static string GetPersistedBlobSizesRoot(string dotGSDRoot)
        {
            return GetPersistedValue(dotGSDRoot, BlobSizesRootKey);
        }

        public static string ReadAllTextFromWriteLockedFile(string filename)
        {
            // File.ReadAllText and others attempt to open for read and FileShare.None, which always fail on
            // the placeholder db and other files that open for write and only share read access
            using (StreamReader reader = new StreamReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                return reader.ReadToEnd();
            }
        }

        public static string GetInternalParameter(string maintenanceJob = "null", string packfileMaintenanceBatchSize = "null")
        {
            return $"\"{{\\\"ServiceName\\\":\\\"{GSDServiceProcess.TestServiceName}\\\"," +
                    "\\\"StartedByService\\\":false," +
                    $"\\\"MaintenanceJob\\\":{maintenanceJob}," +
                    $"\\\"PackfileMaintenanceBatchSize\\\":{packfileMaintenanceBatchSize}}}\"";
        }

        private static string GetModifiedPathsContents(GSDFunctionalTestEnlistment enlistment, FileSystemRunner fileSystem)
        {
            enlistment.WaitForBackgroundOperations();
            string modifiedPathsDatabase = Path.Combine(enlistment.DotGSDRoot, TestConstants.Databases.ModifiedPaths);
            modifiedPathsDatabase.ShouldBeAFile(fileSystem);
            return GSDHelpers.ReadAllTextFromWriteLockedFile(modifiedPathsDatabase);
        }

        private static byte[] StringToShaBytes(string sha)
        {
            byte[] shaBytes = new byte[20];

            string upperCaseSha = sha.ToUpper();
            int stringIndex = 0;
            for (int i = 0; i < 20; ++i)
            {
                stringIndex = i * 2;
                char firstChar = sha[stringIndex];
                char secondChar = sha[stringIndex + 1];
                shaBytes[i] = (byte)(CharToByte(firstChar) << 4 | CharToByte(secondChar));
            }

            return shaBytes;
        }

        private static byte CharToByte(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return (byte)(c - '0');
            }

            if (c >= 'A' && c <= 'F')
            {
                return (byte)(10 + (c - 'A'));
            }

            Assert.Fail($"Invalid character c: {c}");

            return 0;
        }

        private static string GetPersistedValue(string dotGSDRoot, string key)
        {
            string metadataPath = Path.Combine(dotGSDRoot, RepoMetadataName);
            string json;
            using (FileStream fs = new FileStream(metadataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (StreamReader reader = new StreamReader(fs))
            {
                while (!reader.EndOfStream)
                {
                    json = reader.ReadLine();
                    json.Substring(0, 2).ShouldEqual("A ");

                    KeyValuePair<string, string> kvp = JsonConvert.DeserializeObject<KeyValuePair<string, string>>(json.Substring(2));
                    if (kvp.Key == key)
                    {
                        return kvp.Value;
                    }
                }
            }

            return null;
        }

        private static void SavePersistedValue(string dotGSDRoot, string key, string value)
        {
            string metadataPath = Path.Combine(dotGSDRoot, RepoMetadataName);

            Dictionary<string, string> repoMetadata = new Dictionary<string, string>();
            string json;
            using (FileStream fs = new FileStream(metadataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (StreamReader reader = new StreamReader(fs))
            {
                while (!reader.EndOfStream)
                {
                    json = reader.ReadLine();
                    json.Substring(0, 2).ShouldEqual("A ");

                    KeyValuePair<string, string> kvp = JsonConvert.DeserializeObject<KeyValuePair<string, string>>(json.Substring(2));
                    repoMetadata.Add(kvp.Key, kvp.Value);
                }
            }

            repoMetadata[key] = value;

            string newRepoMetadataContents = string.Empty;

            foreach (KeyValuePair<string, string> kvp in repoMetadata)
            {
                newRepoMetadataContents += "A " + JsonConvert.SerializeObject(kvp).Trim() + "\r\n";
            }

            File.WriteAllText(metadataPath, newRepoMetadataContents);
        }
    }
}
