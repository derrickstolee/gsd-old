using GSD.Platform.POSIX;

namespace GSD.Hooks.HooksPlatform
{
    public static partial class GSDHooksPlatform
    {
        public static bool IsElevated()
        {
            return POSIXPlatform.IsElevatedImplementation();
        }

        public static bool IsProcessActive(int processId)
        {
            return POSIXPlatform.IsProcessActiveImplementation(processId);
        }

        public static bool IsConsoleOutputRedirectedToFile()
        {
            return POSIXPlatform.IsConsoleOutputRedirectedToFileImplementation();
        }

        public static bool TryGetNormalizedPath(string path, out string normalizedPath, out string errorMessage)
        {
            return POSIXFileSystem.TryGetNormalizedPathImplementation(path, out normalizedPath, out errorMessage);
        }
    }
}
