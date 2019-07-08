using CommandLine;
using GSD.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GSD.CommandLine
{
    [Verb(LogVerb.LogVerbName, HelpText = "Show the most recent GSD log files")]
    public class LogVerb : GSDVerb
    {
        private const string LogVerbName = "log";
        private static readonly int LogNameConsoleOutputFormatWidth = GetMaxLogNameLength();

        [Value(
            0,
            Required = false,
            Default = "",
            MetaName = "Enlistment Root Path",
            HelpText = "Full or relative path to the GSD enlistment root")]
        public override string EnlistmentRootPathParameter { get; set; }

        [Option(
            "type",
            Default = null,
            HelpText = "The type of log file to display on the console")]
        public string LogType { get; set; }

        protected override string VerbName
        {
            get { return LogVerbName; }
        }

        public override void Execute()
        {
            this.ValidatePathParameter(this.EnlistmentRootPathParameter);

            this.Output.WriteLine("Most recent log files:");

            string errorMessage;
            string enlistmentRoot;
            if (!GSDPlatform.Instance.TryGetGSDEnlistmentRoot(this.EnlistmentRootPathParameter, out enlistmentRoot, out errorMessage))
            {
                this.ReportErrorAndExit(
                    "Error: '{0}' is not a valid GSD enlistment",
                    this.EnlistmentRootPathParameter);
            }

            string gvfsLogsRoot = Path.Combine(
                enlistmentRoot,
                GSDPlatform.Instance.Constants.DotGSDRoot,
                GSDConstants.DotGSD.LogName);

            if (this.LogType == null)
            {
                this.DisplayMostRecent(gvfsLogsRoot, GSDConstants.LogFileTypes.Clone);

                // By using MountPrefix ("mount") DisplayMostRecent will display either mount_verb, mount_upgrade, or mount_process, whichever is more recent
                this.DisplayMostRecent(gvfsLogsRoot, GSDConstants.LogFileTypes.MountPrefix);
                this.DisplayMostRecent(gvfsLogsRoot, GSDConstants.LogFileTypes.Prefetch);
                this.DisplayMostRecent(gvfsLogsRoot, GSDConstants.LogFileTypes.Dehydrate);
                this.DisplayMostRecent(gvfsLogsRoot, GSDConstants.LogFileTypes.Repair);

                string serviceLogsRoot = Path.Combine(
                    GSDPlatform.Instance.GetDataRootForGSDComponent(GSDConstants.Service.ServiceName),
                    GSDConstants.Service.LogDirectory);
                this.DisplayMostRecent(serviceLogsRoot, GSDConstants.LogFileTypes.Service);

                this.DisplayMostRecent(ProductUpgraderInfo.GetLogDirectoryPath(), GSDConstants.LogFileTypes.UpgradePrefix);
            }
            else
            {
                string logFile = FindNewestFileInFolder(gvfsLogsRoot, this.LogType);
                if (logFile == null)
                {
                    this.ReportErrorAndExit("No log file found");
                }
                else
                {
                    foreach (string line in File.ReadAllLines(logFile))
                    {
                        this.Output.WriteLine(line);
                    }
                }
            }
        }

        private static string FindNewestFileInFolder(string folderName, string logFileType)
        {
            string logFilePattern = GetLogFilePatternForType(logFileType);

            DirectoryInfo logDirectory = new DirectoryInfo(folderName);
            if (!logDirectory.Exists)
            {
                return null;
            }

            FileInfo[] files = logDirectory.GetFiles(logFilePattern ?? "*");
            if (files.Length == 0)
            {
                return null;
            }

            return
                files
                .OrderByDescending(fileInfo => fileInfo.CreationTime)
                .First()
                .FullName;
        }

        private static string GetLogFilePatternForType(string logFileType)
        {
            return "gvfs_" + logFileType + "_*.log";
        }

        private static int GetMaxLogNameLength()
        {
            List<string> lognames = new List<string>
            {
                GSDConstants.LogFileTypes.Clone,
                GSDConstants.LogFileTypes.MountPrefix,
                GSDConstants.LogFileTypes.Prefetch,
                GSDConstants.LogFileTypes.Dehydrate,
                GSDConstants.LogFileTypes.Repair,
                GSDConstants.LogFileTypes.Service,
                GSDConstants.LogFileTypes.UpgradePrefix,
            };

            return lognames.Max(s => s.Length) + 1;
        }

        private void DisplayMostRecent(string logFolder, string logFileType)
        {
            string logFile = FindNewestFileInFolder(logFolder, logFileType);
            this.Output.WriteLine(
                $"  {{0, -{LogNameConsoleOutputFormatWidth}}}: {{1}}",
                logFileType,
                logFile == null ? "None" : logFile);
        }
    }
}
