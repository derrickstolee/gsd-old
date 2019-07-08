using GSD.Platform.Mac;

namespace GSD.Hooks.HooksPlatform
{
    public static partial class GSDHooksPlatform
    {
        public static string GetDataRootForGSD()
        {
            return MacPlatform.GetDataRootForGSDImplementation();
        }

        public static string GetUpgradeHighestAvailableVersionDirectory()
        {
            return MacPlatform.GetUpgradeHighestAvailableVersionDirectoryImplementation();
        }

        public static bool TryGetGSDEnlistmentRoot(string directory, out string enlistmentRoot, out string errorMessage)
        {
            return MacPlatform.TryGetGSDEnlistmentRootImplementation(directory, out enlistmentRoot, out errorMessage);
        }

        public static string GetNamedPipeName(string enlistmentRoot)
        {
            return MacPlatform.GetNamedPipeNameImplementation(enlistmentRoot);
        }
    }
}
