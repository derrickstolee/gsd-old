using CommandLine;
using GSD.Common;
using GSD.Common.FileSystem;
using GSD.Common.Git;
using GSD.Common.Http;
using GSD.Common.NamedPipes;
using GSD.Common.Tracing;
using GSD.DiskLayoutUpgrades;
using System;
using System.IO;

namespace GSD.CommandLine
{
    [Verb(MountVerb.MountVerbName, HelpText = "Mount a GSD virtual repo")]
    public class MountVerb : GSDVerb.ForExistingEnlistment
    {
        private const string MountVerbName = "mount";

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

        public bool SkipMountedCheck { get; set; }
        public bool SkipVersionCheck { get; set; }
        public CacheServerInfo ResolvedCacheServer { get; set; }
        public ServerGSDConfig DownloadedGSDConfig { get; set; }

        protected override string VerbName
        {
            get { return MountVerbName; }
        }

        public override void InitializeDefaultParameterValues()
        {
            this.Verbosity = GSDConstants.VerbParameters.Mount.DefaultVerbosity;
            this.KeywordsCsv = GSDConstants.VerbParameters.Mount.DefaultKeywords;
        }

        protected override void PreCreateEnlistment()
        {
            string errorMessage;
            string enlistmentRoot;
            if (!GSDPlatform.Instance.TryGetGSDEnlistmentRoot(this.EnlistmentRootPathParameter, out enlistmentRoot, out errorMessage))
            {
                this.ReportErrorAndExit("Error: '{0}' is not a valid GSD enlistment", this.EnlistmentRootPathParameter);
            }

            if (!this.SkipMountedCheck)
            {
                if (this.IsExistingPipeListening(enlistmentRoot))
                {
                    this.ReportErrorAndExit(tracer: null, exitCode: ReturnCode.Success, error: $"The repo at '{enlistmentRoot}' is already mounted.");
                }
            }

            if (!DiskLayoutUpgrade.TryRunAllUpgrades(enlistmentRoot))
            {
                this.ReportErrorAndExit("Failed to upgrade repo disk layout. " + ConsoleHelper.GetGSDLogMessage(enlistmentRoot));
            }

            string error;
            if (!DiskLayoutUpgrade.TryCheckDiskLayoutVersion(tracer: null, enlistmentRoot: enlistmentRoot, error: out error))
            {
                this.ReportErrorAndExit("Error: " + error);
            }
        }

        protected override void Execute(GSDEnlistment enlistment)
        {
            string errorMessage = null;
            string mountExecutableLocation = null;
            using (JsonTracer tracer = new JsonTracer(GSDConstants.GSDEtwProviderName, "ExecuteMount"))
            {
                PhysicalFileSystem fileSystem = new PhysicalFileSystem();
                GitRepo gitRepo = new GitRepo(tracer, enlistment, fileSystem);
                GSDContext context = new GSDContext(tracer, fileSystem, gitRepo, enlistment);

                if (!HooksInstaller.InstallHooks(context, out errorMessage))
                {
                    this.ReportErrorAndExit("Error installing hooks: " + errorMessage);
                }

                CacheServerInfo cacheServer = this.ResolvedCacheServer ?? CacheServerResolver.GetCacheServerFromConfig(enlistment);

                tracer.AddLogFileEventListener(
                    GSDEnlistment.GetNewGSDLogFileName(enlistment.GSDLogsRoot, GSDConstants.LogFileTypes.MountVerb),
                    EventLevel.Verbose,
                    Keywords.Any);
                tracer.WriteStartEvent(
                    enlistment.EnlistmentRoot,
                    enlistment.RepoUrl,
                    cacheServer.Url,
                    new EventMetadata
                    {
                        { "Unattended", this.Unattended },
                        { "IsElevated", GSDPlatform.Instance.IsElevated() },
                        { "NamedPipeName", enlistment.NamedPipeName },
                        { nameof(this.EnlistmentRootPathParameter), this.EnlistmentRootPathParameter },
                    });

                RetryConfig retryConfig = null;
                ServerGSDConfig serverGSDConfig = this.DownloadedGSDConfig;
                if (!this.SkipVersionCheck)
                {
                    string authErrorMessage;
                    if (!this.TryAuthenticate(tracer, enlistment, out authErrorMessage))
                    {
                        this.Output.WriteLine("    WARNING: " + authErrorMessage);
                        this.Output.WriteLine("    Mount will proceed, but new files cannot be accessed until GSD can authenticate.");
                    }

                    if (serverGSDConfig == null)
                    {
                        if (retryConfig == null)
                        {
                            retryConfig = this.GetRetryConfig(tracer, enlistment);
                        }

                        serverGSDConfig = this.QueryGSDConfig(tracer, enlistment, retryConfig);
                    }

                    this.ValidateClientVersions(tracer, enlistment, serverGSDConfig, showWarnings: true);

                    CacheServerResolver cacheServerResolver = new CacheServerResolver(tracer, enlistment);
                    cacheServer = cacheServerResolver.ResolveNameFromRemote(cacheServer.Url, serverGSDConfig);
                    this.Output.WriteLine("Configured cache server: " + cacheServer);
                }

                this.InitializeLocalCacheAndObjectsPaths(tracer, enlistment, retryConfig, serverGSDConfig, cacheServer);

                if (!this.ShowStatusWhileRunning(
                    () => { return this.PerformPreMountValidation(tracer, enlistment, out mountExecutableLocation, out errorMessage); },
                    "Validating repo"))
                {
                    this.ReportErrorAndExit(tracer, errorMessage);
                }

                if (!this.SkipVersionCheck)
                {
                    string error;
                    if (!RepoMetadata.TryInitialize(tracer, enlistment.DotGSDRoot, out error))
                    {
                        this.ReportErrorAndExit(tracer, error);
                    }

                    try
                    {
                        GitProcess git = new GitProcess(enlistment);
                        this.LogEnlistmentInfoAndSetConfigValues(tracer, git, enlistment);
                    }
                    finally
                    {
                        RepoMetadata.Shutdown();
                    }
                }

                if (!this.ShowStatusWhileRunning(
                    () => { return this.TryMount(tracer, enlistment, mountExecutableLocation, out errorMessage); },
                    "Mounting"))
                {
                    this.ReportErrorAndExit(tracer, errorMessage);
                }

                if (!this.Unattended)
                {
                    tracer.RelatedInfo($"{nameof(this.Execute)}: Registering for automount");

                    if (this.ShowStatusWhileRunning(
                        () => { return this.RegisterMount(enlistment, out errorMessage); },
                        "Registering for automount"))
                    {
                        tracer.RelatedInfo($"{nameof(this.Execute)}: Registered for automount");
                    }
                    else
                    {
                        this.Output.WriteLine("    WARNING: " + errorMessage);
                        tracer.RelatedInfo($"{nameof(this.Execute)}: Failed to register for automount");
                    }
                }
            }
        }

        private bool PerformPreMountValidation(ITracer tracer, GSDEnlistment enlistment, out string mountExecutableLocation, out string errorMessage)
        {
            errorMessage = string.Empty;
            mountExecutableLocation = string.Empty;

            // We have to parse these parameters here to make sure they are valid before
            // handing them to the background process which cannot tell the user when they are bad
            EventLevel verbosity;
            Keywords keywords;
            this.ParseEnumArgs(out verbosity, out keywords);

            mountExecutableLocation = Path.Combine(ProcessHelper.GetCurrentProcessLocation(), GSDPlatform.Instance.Constants.MountExecutableName);
            if (!File.Exists(mountExecutableLocation))
            {
                errorMessage = $"Could not find {GSDPlatform.Instance.Constants.MountExecutableName}. You may need to reinstall GSD.";
                return false;
            }

            GitProcess git = new GitProcess(enlistment);
            if (!git.IsValidRepo())
            {
                errorMessage = "The .git folder is missing or has invalid contents";
                return false;
            }

            return true;
        }

        private bool TryMount(ITracer tracer, GSDEnlistment enlistment, string mountExecutableLocation, out string errorMessage)
        {
            if (!GSDVerb.TrySetRequiredGitConfigSettings(enlistment))
            {
                errorMessage = "Unable to configure git repo";
                return false;
            }

            const string ParamPrefix = "--";

            tracer.RelatedInfo($"{nameof(this.TryMount)}: Launching background process('{mountExecutableLocation}') for {enlistment.EnlistmentRoot}");

            GSDPlatform.Instance.StartBackgroundVFS4GProcess(
                tracer,
                mountExecutableLocation,
                new[]
                {
                    enlistment.EnlistmentRoot,
                    ParamPrefix + GSDConstants.VerbParameters.Mount.Verbosity,
                    this.Verbosity,
                    ParamPrefix + GSDConstants.VerbParameters.Mount.Keywords,
                    this.KeywordsCsv,
                    ParamPrefix + GSDConstants.VerbParameters.Mount.StartedByService,
                    this.StartedByService.ToString(),
                    ParamPrefix + GSDConstants.VerbParameters.Mount.StartedByVerb,
                    true.ToString()
                });

            tracer.RelatedInfo($"{nameof(this.TryMount)}: Waiting for repo to be mounted");
            return GSDEnlistment.WaitUntilMounted(tracer, enlistment.EnlistmentRoot, this.Unattended, out errorMessage);
        }

        private bool RegisterMount(GSDEnlistment enlistment, out string errorMessage)
        {
            errorMessage = string.Empty;

            NamedPipeMessages.RegisterRepoRequest request = new NamedPipeMessages.RegisterRepoRequest();
            request.EnlistmentRoot = enlistment.EnlistmentRoot;

            request.OwnerSID = GSDPlatform.Instance.GetCurrentUser();

            using (NamedPipeClient client = new NamedPipeClient(this.ServicePipeName))
            {
                if (!client.Connect())
                {
                    errorMessage = "Unable to register repo because GSD.Service is not responding.";
                    return false;
                }

                try
                {
                    client.SendRequest(request.ToMessage());
                    NamedPipeMessages.Message response = client.ReadResponse();
                    if (response.Header == NamedPipeMessages.RegisterRepoRequest.Response.Header)
                    {
                        NamedPipeMessages.RegisterRepoRequest.Response message = NamedPipeMessages.RegisterRepoRequest.Response.FromMessage(response);

                        if (!string.IsNullOrEmpty(message.ErrorMessage))
                        {
                            errorMessage = message.ErrorMessage;
                            return false;
                        }

                        if (message.State != NamedPipeMessages.CompletionState.Success)
                        {
                            errorMessage = "Unable to register repo. " + errorMessage;
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        errorMessage = string.Format("GSD.Service responded with unexpected message: {0}", response);
                        return false;
                    }
                }
                catch (BrokenPipeException e)
                {
                    errorMessage = "Unable to communicate with GSD.Service: " + e.ToString();
                    return false;
                }
            }
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
    }
}