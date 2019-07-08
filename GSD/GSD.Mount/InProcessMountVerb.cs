using CommandLine;
using GSD.Common;
using GSD.Common.Git;
using GSD.Common.Http;
using GSD.Common.Tracing;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace GSD.Mount
{
    [Verb("mount", HelpText = "Starts the background mount process")]
    public class InProcessMountVerb
    {
        private TextWriter output;

        public InProcessMountVerb()
        {
            this.output = Console.Out;
            this.ReturnCode = ReturnCode.Success;

            this.InitializeDefaultParameterValues();
        }

        public ReturnCode ReturnCode { get; private set; }

        [Option(
            'v',
            GSDConstants.VerbParameters.Mount.Verbosity,
            Default = GSDConstants.VerbParameters.Mount.DefaultVerbosity,
            Required = false,
            HelpText = "Sets the verbosity of console logging. Accepts: Verbose, Informational, Warning, Error")]
        public string Verbosity { get; set; }

        [Option(
            'k',
            GSDConstants.VerbParameters.Mount.Keywords,
            Default = GSDConstants.VerbParameters.Mount.DefaultKeywords,
            Required = false,
            HelpText = "A CSV list of logging filter keywords. Accepts: Any, Network")]
        public string KeywordsCsv { get; set; }

        [Option(
            'd',
            GSDConstants.VerbParameters.Mount.DebugWindow,
            Default = false,
            Required = false,
            HelpText = "Show the debug window.  By default, all output is written to a log file and no debug window is shown.")]
        public bool ShowDebugWindow { get; set; }

        [Option(
            's',
            GSDConstants.VerbParameters.Mount.StartedByService,
            Default = "false",
            Required = false,
            HelpText = "Service initiated mount.")]
        public string StartedByService { get; set; }

        [Option(
            'b',
            GSDConstants.VerbParameters.Mount.StartedByVerb,
            Default = false,
            Required = false,
            HelpText = "Verb initiated mount.")]
        public bool StartedByVerb { get; set; }

        [Value(
                0,
                Required = true,
                MetaName = "Enlistment Root Path",
                HelpText = "Full or relative path to the GSD enlistment root")]
        public string EnlistmentRootPathParameter { get; set; }

        public void InitializeDefaultParameterValues()
        {
            this.Verbosity = GSDConstants.VerbParameters.Mount.DefaultVerbosity;
            this.KeywordsCsv = GSDConstants.VerbParameters.Mount.DefaultKeywords;
        }

        public void Execute()
        {
            if (this.StartedByVerb)
            {
                // If this process was started by a verb it means that StartBackgroundVFS4GProcess was used
                // and we should be running in the background.  PrepareProcessToRunInBackground will perform
                // any platform specific preparation required to run as a background process.
                GSDPlatform.Instance.PrepareProcessToRunInBackground();
            }

            GSDEnlistment enlistment = this.CreateEnlistment(this.EnlistmentRootPathParameter);

            EventLevel verbosity;
            Keywords keywords;
            this.ParseEnumArgs(out verbosity, out keywords);

            JsonTracer tracer = this.CreateTracer(enlistment, verbosity, keywords);

            CacheServerInfo cacheServer = CacheServerResolver.GetCacheServerFromConfig(enlistment);

            tracer.WriteStartEvent(
                enlistment.EnlistmentRoot,
                enlistment.RepoUrl,
                cacheServer.Url,
                new EventMetadata
                {
                    { "IsElevated", GSDPlatform.Instance.IsElevated() },
                    { nameof(this.EnlistmentRootPathParameter), this.EnlistmentRootPathParameter },
                    { nameof(this.StartedByService), this.StartedByService },
                    { nameof(this.StartedByVerb), this.StartedByVerb },
                });

            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                this.UnhandledGSDExceptionHandler(tracer, sender, e);
            };

            string error;
            RetryConfig retryConfig;
            if (!RetryConfig.TryLoadFromGitConfig(tracer, enlistment, out retryConfig, out error))
            {
                this.ReportErrorAndExit(tracer, "Failed to determine GSD timeout and max retries: " + error);
            }

            GitStatusCacheConfig gitStatusCacheConfig;
            if (!GitStatusCacheConfig.TryLoadFromGitConfig(tracer, enlistment, out gitStatusCacheConfig, out error))
            {
                tracer.RelatedWarning("Failed to determine GSD status cache backoff time: " + error);
                gitStatusCacheConfig = GitStatusCacheConfig.DefaultConfig;
            }

            InProcessMount mountHelper = new InProcessMount(tracer, enlistment, cacheServer, retryConfig, gitStatusCacheConfig, this.ShowDebugWindow);

            try
            {
                mountHelper.Mount(verbosity, keywords);
            }
            catch (Exception ex)
            {
                this.ReportErrorAndExit(tracer, "Failed to mount: {0}", ex.Message);
            }
        }

        private void UnhandledGSDExceptionHandler(ITracer tracer, object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;

            EventMetadata metadata = new EventMetadata();
            metadata.Add("Exception", exception.ToString());
            metadata.Add("IsTerminating", e.IsTerminating);
            tracer.RelatedError(metadata, "UnhandledGSDExceptionHandler caught unhandled exception");
        }

        private JsonTracer CreateTracer(GSDEnlistment enlistment, EventLevel verbosity, Keywords keywords)
        {
            JsonTracer tracer = new JsonTracer(GSDConstants.GSDEtwProviderName, "GSDMount", enlistment.GetEnlistmentId(), enlistment.GetMountId());
            tracer.AddLogFileEventListener(
                GSDEnlistment.GetNewGSDLogFileName(enlistment.GSDLogsRoot, GSDConstants.LogFileTypes.MountProcess),
                verbosity,
                keywords);
            if (this.ShowDebugWindow)
            {
                tracer.AddDiagnosticConsoleEventListener(verbosity, keywords);
            }

            return tracer;
        }

        private void ParseEnumArgs(out EventLevel verbosity, out Keywords keywords)
        {
            if (!Enum.TryParse(this.KeywordsCsv, out keywords))
            {
                this.ReportErrorAndExit("Error: Invalid logging filter keywords: " + this.KeywordsCsv);
            }

            if (!Enum.TryParse(this.Verbosity, out verbosity))
            {
                this.ReportErrorAndExit("Error: Invalid logging verbosity: " + this.Verbosity);
            }
        }

        private GSDEnlistment CreateEnlistment(string enlistmentRootPath)
        {
            string gitBinPath = GSDPlatform.Instance.GitInstallation.GetInstalledGitBinPath();
            if (string.IsNullOrWhiteSpace(gitBinPath))
            {
                this.ReportErrorAndExit("Error: " + GSDConstants.GitIsNotInstalledError);
            }

            GSDEnlistment enlistment = null;
            try
            {
                enlistment = GSDEnlistment.CreateFromDirectory(enlistmentRootPath, gitBinPath, ProcessHelper.GetCurrentProcessLocation(), authentication: null);
            }
            catch (InvalidRepoException e)
            {
                this.ReportErrorAndExit(
                    "Error: '{0}' is not a valid GSD enlistment. {1}",
                    enlistmentRootPath,
                    e.Message);
            }

            return enlistment;
        }

        private void ReportErrorAndExit(string error, params object[] args)
        {
            this.ReportErrorAndExit(null, error, args);
        }

        private void ReportErrorAndExit(ITracer tracer, string error, params object[] args)
        {
            if (tracer != null)
            {
                tracer.RelatedError(error, args);
            }

            if (error != null)
            {
                this.output.WriteLine(error, args);
            }

            if (this.ShowDebugWindow)
            {
                Console.WriteLine("\nPress Enter to Exit");
                Console.ReadLine();
            }

            this.ReturnCode = ReturnCode.GenericError;
            throw new MountAbortedException(this);
        }
    }
}
