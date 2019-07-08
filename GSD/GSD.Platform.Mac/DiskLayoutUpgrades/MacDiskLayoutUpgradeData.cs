using GSD.Common;
using GSD.DiskLayoutUpgrades;
using GSD.Platform.Mac.DiskLayoutUpgrades;

namespace GSD.Platform.Mac
{
    public class MacDiskLayoutUpgradeData : IDiskLayoutUpgradeData
    {
        public DiskLayoutUpgrade[] Upgrades
        {
            get
            {
                return new DiskLayoutUpgrade[]
                {
                    new DiskLayout18to19Upgrade_SqlitePlacholders(),
                };
            }
        }

        public DiskLayoutVersion Version => new DiskLayoutVersion(
            currentMajorVersion: 19,
            currentMinorVersion: 0,
            minimumSupportedMajorVersion: 18);

        public bool TryParseLegacyDiskLayoutVersion(string dotGSDPath, out int majorVersion)
        {
            majorVersion = 0;
            return false;
        }
    }
}
