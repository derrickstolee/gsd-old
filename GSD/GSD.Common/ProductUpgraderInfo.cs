using GSD.Common.FileSystem;
using GSD.Common.Tracing;
using System;
using System.IO;

namespace GSD.Common
{
    public partial class ProductUpgraderInfo
    {
        private ITracer tracer;
        private PhysicalFileSystem fileSystem;

        public ProductUpgraderInfo(ITracer tracer, PhysicalFileSystem fileSystem)
        {
            this.tracer = tracer;
            this.fileSystem = fileSystem;
        }

        public static string CurrentGSDVersion()
        {
            return ProcessHelper.GetCurrentProcessVersion();
        }

        public static string GetUpgradeProtectedDataDirectory()
        {
            return GSDPlatform.Instance.GetUpgradeProtectedDataDirectory();
        }

        public static string GetUpgradeApplicationDirectory()
        {
            return Path.Combine(
                GetUpgradeProtectedDataDirectory(),
                ProductUpgraderInfo.ApplicationDirectory);
        }

        public static string GetParentLogDirectoryPath()
        {
            return GSDPlatform.Instance.GetUpgradeLogDirectoryParentDirectory();
        }

        public static string GetLogDirectoryPath()
        {
            return Path.Combine(
                GSDPlatform.Instance.GetUpgradeLogDirectoryParentDirectory(),
                ProductUpgraderInfo.LogDirectory);
        }

        public static string GetAssetDownloadsPath()
        {
            return Path.Combine(
                GSDPlatform.Instance.GetUpgradeProtectedDataDirectory(),
                ProductUpgraderInfo.DownloadDirectory);
        }

        public static string GetHighestAvailableVersionDirectory()
        {
            return GSDPlatform.Instance.GetUpgradeHighestAvailableVersionDirectory();
        }

        public void DeleteAllInstallerDownloads()
        {
            try
            {
                this.fileSystem.DeleteDirectory(GetAssetDownloadsPath());
            }
            catch (Exception ex)
            {
                if (this.tracer != null)
                {
                    this.tracer.RelatedError($"{nameof(this.DeleteAllInstallerDownloads)}: Could not remove directory: {ProductUpgraderInfo.GetAssetDownloadsPath()}.{ex.ToString()}");
                }
            }
        }

        public void RecordHighestAvailableVersion(Version highestAvailableVersion)
        {
            string highestAvailableVersionFile = GetHighestAvailableVersionFilePath(GetHighestAvailableVersionDirectory());

            if (highestAvailableVersion == null)
            {
                if (this.fileSystem.FileExists(highestAvailableVersionFile))
                {
                    this.fileSystem.DeleteFile(highestAvailableVersionFile);

                    if (this.tracer != null)
                    {
                        this.tracer.RelatedInfo($"{nameof(this.RecordHighestAvailableVersion)}: Deleted upgrade reminder marker file");
                    }
                }
            }
            else
            {
                this.fileSystem.WriteAllText(highestAvailableVersionFile, highestAvailableVersion.ToString());

                if (this.tracer != null)
                {
                    this.tracer.RelatedInfo($"{nameof(this.RecordHighestAvailableVersion)}: Created upgrade reminder marker file");
                }
            }
        }
    }
}
