using CommandLine;
using GSD.Common;
using GSD.Common.FileSystem;
using GSD.Common.Git;
using GSD.Common.Http;
using GSD.Common.Maintenance;
using GSD.Common.Tracing;
using GSD.DiskLayoutUpgrades;
using System;
using System.IO;
using System.Text;

namespace GSD.CommandLine
{
    [Verb(DehydrateVerb.DehydrateVerbName, HelpText = "EXPERIMENTAL FEATURE - Fully dehydrate a GSD repo")]
    public class DehydrateVerb : GSDVerb.ForExistingEnlistment
    {
        private const string DehydrateVerbName = "dehydrate";

        [Option(
            "confirm",
            Default = false,
            Required = false,
            HelpText = "Pass in this flag to actually do the dehydrate")]
        public bool Confirmed { get; set; }

        [Option(
            "no-status",
            Default = false,
            Required = false,
            HelpText = "Skip 'git status' before dehydrating")]
        public bool NoStatus { get; set; }

        protected override string VerbName
        {
            get { return DehydrateVerb.DehydrateVerbName; }
        }

        protected override void Execute(GSDEnlistment enlistment)
        {
            using (JsonTracer tracer = new JsonTracer(GSDConstants.GSDEtwProviderName, "Dehydrate"))
            {
                tracer.AddLogFileEventListener(
                    GSDEnlistment.GetNewGSDLogFileName(enlistment.GSDLogsRoot, GSDConstants.LogFileTypes.Dehydrate),
                    EventLevel.Informational,
                    Keywords.Any);
                tracer.WriteStartEvent(
                    enlistment.EnlistmentRoot,
                    enlistment.RepoUrl,
                    CacheServerResolver.GetUrlFromConfig(enlistment),
                    new EventMetadata
                    {
                        { "Confirmed", this.Confirmed },
                        { "NoStatus", this.NoStatus },
                        { "NamedPipeName", enlistment.NamedPipeName },
                        { nameof(this.EnlistmentRootPathParameter), this.EnlistmentRootPathParameter },
                    });

                // This is only intended to be run by functional tests
                if (this.MaintenanceJob != null)
                {
                    this.InitializeLocalCacheAndObjectsPaths(tracer, enlistment, retryConfig: null, serverGSDConfig: null, cacheServer: null);
                    PhysicalFileSystem fileSystem = new PhysicalFileSystem();
                    using (GitRepo gitRepo = new GitRepo(tracer, enlistment, fileSystem))
                    using (GSDContext context = new GSDContext(tracer, fileSystem, gitRepo, enlistment))
                    {
                        switch (this.MaintenanceJob)
                        {
                            case "LooseObjects":
                                (new LooseObjectsStep(context, forceRun: true)).Execute();
                                return;

                            case "PackfileMaintenance":
                                (new PackfileMaintenanceStep(
                                    context,
                                    forceRun: true,
                                    batchSize: this.PackfileMaintenanceBatchSize ?? PackfileMaintenanceStep.DefaultBatchSize)).Execute();
                                return;

                            case "PostFetch":
                                (new PostFetchStep(context, new System.Collections.Generic.List<string>(), requireObjectCacheLock: false)).Execute();
                                return;

                            default:
                                this.ReportErrorAndExit($"Unknown maintenance job requested: {this.MaintenanceJob}");
                                break;
                        }
                    }
                }

                if (!this.Confirmed)
                {
                    this.Output.WriteLine(
@"WARNING: THIS IS AN EXPERIMENTAL FEATURE

Dehydrate will back up your src folder, and then create a new, empty src folder 
with a fresh virtualization of the repo. All of your downloaded objects, branches, 
and siblings of the src folder will be preserved. Your modified working directory 
files will be moved to the backup, and your new working directory will not have 
any of your uncommitted changes.

Before you dehydrate, make sure you have committed any working directory changes 
you want to keep. If you choose not to, you can still find your uncommitted changes 
in the backup folder, but it will be harder to find them because 'git status' 
will not work in the backup.

To actually execute the dehydrate, run 'gvfs dehydrate --confirm' from the parent 
of your enlistment's src folder.
");

                    return;
                }

                this.CheckGitStatus(tracer, enlistment);

                string backupRoot = Path.GetFullPath(Path.Combine(enlistment.EnlistmentRoot, "dehydrate_backup", DateTime.Now.ToString("yyyyMMdd_HHmmss")));
                this.Output.WriteLine();
                this.WriteMessage(tracer, "Starting dehydration. All of your existing files will be backed up in " + backupRoot);
                this.WriteMessage(tracer, "WARNING: If you abort the dehydrate after this point, the repo may become corrupt");
                this.Output.WriteLine();

                this.Unmount(tracer);

                string error;
                if (!DiskLayoutUpgrade.TryCheckDiskLayoutVersion(tracer, enlistment.EnlistmentRoot, out error))
                {
                    this.ReportErrorAndExit(tracer, error);
                }

                RetryConfig retryConfig;
                if (!RetryConfig.TryLoadFromGitConfig(tracer, enlistment, out retryConfig, out error))
                {
                    this.ReportErrorAndExit(tracer, "Failed to determine GSD timeout and max retries: " + error);
                }

                string errorMessage;
                if (!this.TryAuthenticate(tracer, enlistment, out errorMessage))
                {
                    this.ReportErrorAndExit(tracer, errorMessage);
                }

                // Local cache and objects paths are required for TryDownloadGitObjects
                this.InitializeLocalCacheAndObjectsPaths(tracer, enlistment, retryConfig, serverGSDConfig: null, cacheServer: null);
            }
        }

        private void CheckGitStatus(ITracer tracer, GSDEnlistment enlistment)
        {
            if (!this.NoStatus)
            {
                this.WriteMessage(tracer, "Running git status before dehydrating to make sure you don't have any pending changes.");
                this.WriteMessage(tracer, "If this takes too long, you can abort and run dehydrate with --no-status to skip this safety check.");
                this.Output.WriteLine();

                bool isMounted = false;
                GitProcess.Result statusResult = null;
                if (!this.ShowStatusWhileRunning(
                    () =>
                    {
                        if (this.ExecuteGSDVerb<StatusVerb>(tracer) != ReturnCode.Success)
                        {
                            return false;
                        }

                        isMounted = true;

                        GitProcess git = new GitProcess(enlistment);
                        statusResult = git.Status(allowObjectDownloads: false, useStatusCache: false);
                        if (statusResult.ExitCodeIsFailure)
                        {
                            return false;
                        }

                        if (!statusResult.Output.Contains("nothing to commit, working tree clean"))
                        {
                            return false;
                        }

                        return true;
                    },
                    "Running git status",
                    suppressGvfsLogMessage: true))
                {
                    this.Output.WriteLine();

                    if (!isMounted)
                    {
                        this.WriteMessage(tracer, "Failed to run git status because the repo is not mounted");
                        this.WriteMessage(tracer, "Either mount first, or run with --no-status");
                    }
                    else if (statusResult.ExitCodeIsFailure)
                    {
                        this.WriteMessage(tracer, "Failed to run git status: " + statusResult.Errors);
                    }
                    else
                    {
                        this.WriteMessage(tracer, statusResult.Output);
                        this.WriteMessage(tracer, "git status reported that you have dirty files");
                        this.WriteMessage(tracer, "Either commit your changes or run dehydrate with --no-status");
                    }

                    this.ReportErrorAndExit(tracer, "Dehydrate was aborted");
                }
            }
        }

        private void Unmount(ITracer tracer)
        {
            if (!this.ShowStatusWhileRunning(
                () =>
                {
                    return
                        this.ExecuteGSDVerb<StatusVerb>(tracer) != ReturnCode.Success ||
                        this.ExecuteGSDVerb<UnmountVerb>(tracer) == ReturnCode.Success;
                },
                "Unmounting",
                suppressGvfsLogMessage: true))
            {
                this.ReportErrorAndExit(tracer, "Unable to unmount.");
            }
        }

        private void WriteMessage(ITracer tracer, string message)
        {
            this.Output.WriteLine(message);
            tracer.RelatedEvent(
                EventLevel.Informational,
                "Dehydrate",
                new EventMetadata
                {
                    { TracingConstants.MessageKey.InfoMessage, message }
                });
        }

        private ReturnCode ExecuteGSDVerb<TVerb>(ITracer tracer)
            where TVerb : GSDVerb, new()
        {
            try
            {
                ReturnCode returnCode;
                StringBuilder commandOutput = new StringBuilder();
                using (StringWriter writer = new StringWriter(commandOutput))
                {
                    returnCode = this.Execute<TVerb>(this.EnlistmentRootPathParameter, verb => verb.Output = writer);
                }

                tracer.RelatedEvent(
                    EventLevel.Informational,
                    typeof(TVerb).Name,
                    new EventMetadata
                    {
                        { "Output", commandOutput.ToString() },
                        { "ReturnCode", returnCode }
                    });

                return returnCode;
            }
            catch (Exception e)
            {
                tracer.RelatedError(
                    new EventMetadata
                    {
                        { "Verb", typeof(TVerb).Name },
                        { "Exception", e.ToString() }
                    },
                    "ExecuteGSDVerb: Caught exception");

                return ReturnCode.GenericError;
            }
        }
    }
}
