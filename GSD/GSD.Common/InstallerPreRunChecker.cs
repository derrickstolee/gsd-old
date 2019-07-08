using GSD.Common;
using GSD.Common.Git;
using GSD.Common.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace GSD.Upgrader
{
    public class InstallerPreRunChecker
    {
        private ITracer tracer;

        public InstallerPreRunChecker(ITracer tracer, string commandToRerun)
        {
            this.tracer = tracer;
            this.CommandToRerun = commandToRerun;
        }

        protected string CommandToRerun { private get; set; }

        public virtual bool TryRunPreUpgradeChecks(out string consoleError)
        {
            using (ITracer activity = this.tracer.StartActivity(nameof(this.TryRunPreUpgradeChecks), EventLevel.Informational))
            {
                if (this.IsUnattended())
                {
                    consoleError = $"{GSDConstants.UpgradeVerbMessages.GSDUpgrade} is not supported in unattended mode";
                    this.tracer.RelatedWarning($"{nameof(this.TryRunPreUpgradeChecks)}: {consoleError}");
                    return false;
                }

                if (!this.IsGSDUpgradeAllowed(out consoleError))
                {
                    return false;
                }

                activity.RelatedInfo($"Successfully finished pre upgrade checks. Okay to run {GSDConstants.UpgradeVerbMessages.GSDUpgrade}.");
            }

            consoleError = null;
            return true;
        }

        // TODO: Move repo mount calls to GSD.Upgrader project.
        // https://github.com/Microsoft/GSD/issues/293
        public virtual bool TryMountAllGSDRepos(out string consoleError)
        {
            return this.TryRunGSDWithArgs("service --mount-all", out consoleError);
        }

        public virtual bool TryUnmountAllGSDRepos(out string consoleError)
        {
            consoleError = null;
            this.tracer.RelatedInfo("Unmounting any mounted GSD repositories.");

            using (ITracer activity = this.tracer.StartActivity(nameof(this.TryUnmountAllGSDRepos), EventLevel.Informational))
            {
                if (!this.TryRunGSDWithArgs("service --unmount-all", out consoleError))
                {
                    this.tracer.RelatedError($"{nameof(this.TryUnmountAllGSDRepos)}: {consoleError}");
                    return false;
                }

                activity.RelatedInfo("Successfully unmounted repositories.");
            }

            return true;
        }

        public virtual bool IsInstallationBlockedByRunningProcess(out string consoleError)
        {
            consoleError = null;

            // While checking for blocking processes like GSD.Mount immediately after un-mounting,
            // then sometimes GSD.Mount shows up as running. But if the check is done after waiting
            // for some time, then eventually GSD.Mount goes away. The retry loop below is to help
            // account for this delay between the time un-mount call returns and when GSD.Mount
            // actually quits.
            this.tracer.RelatedInfo("Checking if GSD or dependent processes are running.");
            int retryCount = 10;
            HashSet<string> processList = null;
            while (retryCount > 0)
            {
                if (!this.IsBlockingProcessRunning(out processList))
                {
                    break;
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(250));
                retryCount--;
            }

            if (processList.Count > 0)
            {
                consoleError = string.Join(
                    Environment.NewLine,
                    "Blocking processes are running.",
                    $"Run {this.CommandToRerun} again after quitting these processes - " + string.Join(", ", processList.ToArray()));
                this.tracer.RelatedWarning($"{nameof(this.IsInstallationBlockedByRunningProcess)}: {consoleError}");
                return false;
            }

            return true;
        }

        protected virtual bool IsElevated()
        {
            return GSDPlatform.Instance.IsElevated();
        }

        protected virtual bool IsServiceInstalledAndNotRunning()
        {
            GSDPlatform.Instance.IsServiceInstalledAndRunning(GSDConstants.Service.ServiceName, out bool isInstalled, out bool isRunning);

            return isInstalled && !isRunning;
        }

        protected virtual bool IsUnattended()
        {
            return GSDEnlistment.IsUnattended(this.tracer);
        }

        protected virtual bool IsBlockingProcessRunning(out HashSet<string> processes)
        {
            int currentProcessId = Process.GetCurrentProcess().Id;
            Process[] allProcesses = Process.GetProcesses();
            HashSet<string> matchingNames = new HashSet<string>();

            foreach (Process process in allProcesses)
            {
                if (process.Id == currentProcessId || !GSDPlatform.Instance.Constants.UpgradeBlockingProcesses.Contains(process.ProcessName))
                {
                    continue;
                }

                matchingNames.Add(process.ProcessName + " pid:" + process.Id);
            }

            processes = matchingNames;
            return processes.Count > 0;
        }

        protected virtual bool TryRunGSDWithArgs(string args, out string consoleError)
        {
            string gvfsDirectory = ProcessHelper.GetProgramLocation(GSDPlatform.Instance.Constants.ProgramLocaterCommand, GSDPlatform.Instance.Constants.GSDExecutableName);
            if (!string.IsNullOrEmpty(gvfsDirectory))
            {
                string gvfsPath = Path.Combine(gvfsDirectory, GSDPlatform.Instance.Constants.GSDExecutableName);

                ProcessResult processResult = ProcessHelper.Run(gvfsPath, args);
                if (processResult.ExitCode == 0)
                {
                    consoleError = null;
                    return true;
                }
                else
                {
                    consoleError = string.IsNullOrEmpty(processResult.Errors) ? $"`gvfs {args}` failed." : processResult.Errors;
                    return false;
                }
            }
            else
            {
                consoleError = $"Could not locate {GSDPlatform.Instance.Constants.GSDExecutableName}";
                return false;
            }
        }

        private bool IsGSDUpgradeAllowed(out string consoleError)
        {
            bool isConfirmed = string.Equals(this.CommandToRerun, GSDConstants.UpgradeVerbMessages.GSDUpgradeConfirm, StringComparison.OrdinalIgnoreCase);
            string adviceText = null;
            if (!this.IsElevated())
            {
                adviceText = isConfirmed ? $"Run {this.CommandToRerun} again from an elevated command prompt." : $"To install, run {GSDConstants.UpgradeVerbMessages.GSDUpgradeConfirm} from an elevated command prompt.";
                consoleError = string.Join(
                    Environment.NewLine,
                    "The installer needs to be run from an elevated command prompt.",
                    adviceText);
                this.tracer.RelatedWarning($"{nameof(this.IsGSDUpgradeAllowed)}: Upgrade is not installable. {consoleError}");
                return false;
            }

            if (this.IsServiceInstalledAndNotRunning())
            {
                adviceText = isConfirmed ? $"Run `sc start GSD.Service` and run {this.CommandToRerun} again from an elevated command prompt." : $"To install, run `sc start GSD.Service` and run {GSDConstants.UpgradeVerbMessages.GSDUpgradeConfirm} from an elevated command prompt.";
                consoleError = string.Join(
                    Environment.NewLine,
                    "GSD Service is not running.",
                    adviceText);
                this.tracer.RelatedWarning($"{nameof(this.IsGSDUpgradeAllowed)}: Upgrade is not installable. {consoleError}");
                return false;
            }

            consoleError = null;
            return true;
        }
    }
}
