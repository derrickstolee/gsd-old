using GSD.Common;
using GSD.Common.FileSystem;
using GSD.Common.Tracing;
using System.IO;

namespace GSD.Upgrader
{
    public class WindowsUpgradeOrchestrator : UpgradeOrchestrator
    {
        public WindowsUpgradeOrchestrator(
            ProductUpgrader upgrader,
            ITracer tracer,
            PhysicalFileSystem fileSystem,
            InstallerPreRunChecker preRunChecker,
            TextReader input,
            TextWriter output)
            : base(upgrader, tracer, fileSystem, preRunChecker, input, output)
        {
        }

        public WindowsUpgradeOrchestrator(UpgradeOptions options)
            : base(options)
        {
        }

        protected override bool TryMountRepositories(out string consoleError)
        {
            string errorMessage = string.Empty;
            if (this.mount && !this.LaunchInsideSpinner(
                () =>
                {
                    string mountError;
                    if (!this.preRunChecker.TryMountAllGSDRepos(out mountError))
                    {
                        EventMetadata metadata = new EventMetadata();
                        metadata.Add("Upgrade Step", nameof(this.TryMountRepositories));
                        metadata.Add("Mount Error", mountError);
                        this.tracer.RelatedError(metadata, $"{nameof(this.preRunChecker.TryMountAllGSDRepos)} failed.");
                        errorMessage += mountError;
                        return false;
                    }

                    return true;
                },
                "Mounting repositories"))
            {
                consoleError = errorMessage;
                return false;
            }

            consoleError = null;
            return true;
        }
    }
}
