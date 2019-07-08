using GSD.Common;
using GSD.Platform.Windows;

namespace GSD.PlatformLoader
{
    public static class GSDPlatformLoader
    {
        public static void Initialize()
        {
            GSDPlatform.Register(new WindowsPlatform());
            return;
        }
    }
}