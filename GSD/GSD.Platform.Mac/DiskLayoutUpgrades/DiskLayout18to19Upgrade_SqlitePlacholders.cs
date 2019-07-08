using GSD.Common.DiskLayoutUpgrades;

namespace GSD.Platform.Mac.DiskLayoutUpgrades
{
    public class DiskLayout18to19Upgrade_SqlitePlacholders : DiskLayoutUpgrade_SqlitePlaceholders
    {
        protected override int SourceMajorVersion => 18;
    }
}
