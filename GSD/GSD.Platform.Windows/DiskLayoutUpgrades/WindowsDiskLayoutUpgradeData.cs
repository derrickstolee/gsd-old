using GSD.Common;
using GSD.DiskLayoutUpgrades;

namespace GSD.Platform.Windows.DiskLayoutUpgrades
{
    public class WindowsDiskLayoutUpgradeData : IDiskLayoutUpgradeData
    {
        public DiskLayoutUpgrade[] Upgrades
        {
            get
            {
                return new DiskLayoutUpgrade[]
                {
                };
            }
        }

        public DiskLayoutVersion Version => new DiskLayoutVersion(
                    currentMajorVersion: 19,
                    currentMinorVersion: 0,
                    minimumSupportedMajorVersion: 7);

        public bool TryParseLegacyDiskLayoutVersion(string dotGSDPath, out int majorVersion)
        {
            majorVersion = 0;
            return false;
        }
    }
}
