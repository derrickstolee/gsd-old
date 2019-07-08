using GSD.DiskLayoutUpgrades;

namespace GSD.Common
{
    public interface IDiskLayoutUpgradeData
    {
        DiskLayoutUpgrade[] Upgrades { get; }
        DiskLayoutVersion Version { get; }
        bool TryParseLegacyDiskLayoutVersion(string dotGSDPath, out int majorVersion);
    }
}
