using GSD.Common;
using GSD.Platform.Mac;

namespace GSD.PlatformLoader
{
    public static class GSDPlatformLoader
    {
        public static void Initialize()
        {
            GSDPlatform.Register(new MacPlatform());
            return;
        }
    }
}