using System;
using System.IO;
using GSD.Common;
using GSD.Platform.POSIX;

namespace GSD.Platform.Mac
{
    public partial class MacPlatform
    {
        public const string DotGSDRoot = ".gvfs";

        public static string GetDataRootForGSDImplementation()
        {
            return Path.Combine(
                Environment.GetEnvironmentVariable("HOME"),
                "Library",
                "Application Support",
                "GSD");
        }

        public static string GetDataRootForGSDComponentImplementation(string componentName)
        {
            return Path.Combine(GetDataRootForGSDImplementation(), componentName);
        }

        public static bool TryGetGSDEnlistmentRootImplementation(string directory, out string enlistmentRoot, out string errorMessage)
        {
            return POSIXPlatform.TryGetGSDEnlistmentRootImplementation(directory, DotGSDRoot, out enlistmentRoot, out errorMessage);
        }

        public static string GetUpgradeHighestAvailableVersionDirectoryImplementation()
        {
            return GetUpgradeNonProtectedDirectoryImplementation();
        }

        public static string GetUpgradeNonProtectedDirectoryImplementation()
        {
            return Path.Combine(GetDataRootForGSDImplementation(), ProductUpgraderInfo.UpgradeDirectoryName);
        }

        public static string GetNamedPipeNameImplementation(string enlistmentRoot)
        {
            return POSIXPlatform.GetNamedPipeNameImplementation(enlistmentRoot, DotGSDRoot);
        }

        private string GetUpgradeNonProtectedDataDirectory()
        {
            return GetUpgradeNonProtectedDirectoryImplementation();
        }
    }
}
