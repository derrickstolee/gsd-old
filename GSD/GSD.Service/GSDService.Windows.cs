using GSD.Common;
using GSD.Common.FileSystem;
using GSD.Common.NamedPipes;
using GSD.Common.Tracing;
using GSD.Platform.Windows;
using GSD.Service.Handlers;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Threading;

namespace GSD.Service
{
    public class GSDService : ServiceBase
    {
        private const string ServiceNameArgPrefix = "--servicename=";
        private const string EtwArea = nameof(GSDService);

        private JsonTracer tracer;
        private Thread serviceThread;
        private ManualResetEvent serviceStopped;
        private string serviceName;
        private string serviceDataLocation;
        private RepoRegistry repoRegistry;
        private ProductUpgradeTimer productUpgradeTimer;
        private RequestHandler requestHandler;

        public GSDService(JsonTracer tracer)
        {
            this.tracer = tracer;
            this.serviceName = GSDConstants.Service.ServiceName;
            this.CanHandleSessionChangeEvent = true;
            this.productUpgradeTimer = new ProductUpgradeTimer(tracer);
        }

        public void Run()
        {
            try
            {
                EventMetadata metadata = new EventMetadata();
                metadata.Add("Version", ProcessHelper.GetCurrentProcessVersion());
                this.tracer.RelatedEvent(EventLevel.Informational, $"{nameof(GSDService)}_{nameof(this.Run)}", metadata);

                this.repoRegistry = new RepoRegistry(
                    this.tracer,
                    new PhysicalFileSystem(),
                    this.serviceDataLocation,
                    new GSDMountProcess(this.tracer),
                    new NotificationHandler(this.tracer));
                this.repoRegistry.Upgrade();
                this.requestHandler = new RequestHandler(this.tracer, EtwArea, this.repoRegistry);

                string pipeName = GSDPlatform.Instance.GetGSDServiceNamedPipeName(this.serviceName);
                this.tracer.RelatedInfo("Starting pipe server with name: " + pipeName);

                using (NamedPipeServer pipeServer = NamedPipeServer.StartNewServer(
                    pipeName,
                    this.tracer,
                    this.requestHandler.HandleRequest))
                {
                    this.CheckEnableGitStatusCacheTokenFile();

                    // Start product upgrade timer only after attempting to enable prjflt.
                    // On Windows server (where PrjFlt is not inboxed) this helps avoid
                    // a race between TryEnablePrjFlt() and installer pre-check which is
                    // performed by UpgradeTimer in parallel.
                    this.productUpgradeTimer.Start();

                    this.serviceStopped.WaitOne();
                }
            }
            catch (Exception e)
            {
                this.LogExceptionAndExit(e, nameof(this.Run));
            }
        }

        public void StopRunning()
        {
            if (this.serviceStopped == null)
            {
                return;
            }

            try
            {
                if (this.productUpgradeTimer != null)
                {
                    this.productUpgradeTimer.Stop();
                }

                if (this.tracer != null)
                {
                    this.tracer.RelatedInfo("Stopping");
                }

                if (this.serviceStopped != null)
                {
                    this.serviceStopped.Set();
                }

                if (this.serviceThread != null)
                {
                    this.serviceThread.Join();
                    this.serviceThread = null;

                    if (this.serviceStopped != null)
                    {
                        this.serviceStopped.Dispose();
                        this.serviceStopped = null;
                    }
                }
            }
            catch (Exception e)
            {
                this.LogExceptionAndExit(e, nameof(this.StopRunning));
            }
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            try
            {
                base.OnSessionChange(changeDescription);

                if (!GSDEnlistment.IsUnattended(tracer: null))
                {
                    if (changeDescription.Reason == SessionChangeReason.SessionLogon)
                    {
                        this.tracer.RelatedInfo("SessionLogon detected, sessionId: {0}", changeDescription.SessionId);
                        using (ITracer activity = this.tracer.StartActivity("LogonAutomount", EventLevel.Informational))
                        {
                            this.repoRegistry.AutoMountRepos(
                                GSDPlatform.Instance.GetUserIdFromLoginSessionId(changeDescription.SessionId, this.tracer),
                                changeDescription.SessionId);
                            this.repoRegistry.TraceStatus();
                        }
                    }
                    else if (changeDescription.Reason == SessionChangeReason.SessionLogoff)
                    {
                        this.tracer.RelatedInfo("SessionLogoff detected");
                    }
                }
            }
            catch (Exception e)
            {
                this.LogExceptionAndExit(e, nameof(this.OnSessionChange));
            }
        }

        protected override void OnStart(string[] args)
        {
            if (this.serviceThread != null)
            {
                throw new InvalidOperationException("Cannot start service twice in a row.");
            }

            // TODO: 865304 Used for functional tests and development only. Replace with a smarter appConfig-based solution
            string serviceName = args.FirstOrDefault(arg => arg.StartsWith(ServiceNameArgPrefix));
            if (serviceName != null)
            {
                this.serviceName = serviceName.Substring(ServiceNameArgPrefix.Length);
            }

            string serviceLogsDirectoryPath = Path.Combine(
                    GSDPlatform.Instance.GetDataRootForGSDComponent(this.serviceName),
                    GSDConstants.Service.LogDirectory);

            // Create the logs directory explicitly *before* creating a log file event listener to ensure that it
            // and its ancestor directories are created with the correct ACLs.
            this.CreateServiceLogsDirectory(serviceLogsDirectoryPath);
            this.tracer.AddLogFileEventListener(
                GSDEnlistment.GetNewGSDLogFileName(serviceLogsDirectoryPath, GSDConstants.LogFileTypes.Service),
                EventLevel.Verbose,
                Keywords.Any);

            try
            {
                this.serviceDataLocation = GSDPlatform.Instance.GetDataRootForGSDComponent(this.serviceName);
                this.CreateAndConfigureProgramDataDirectories();
                this.Start();
            }
            catch (Exception e)
            {
                this.LogExceptionAndExit(e, nameof(this.OnStart));
            }
        }

        protected override void OnStop()
        {
            try
            {
                this.StopRunning();
            }
            catch (Exception e)
            {
                this.LogExceptionAndExit(e, nameof(this.OnStart));
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.StopRunning();

            if (this.tracer != null)
            {
                this.tracer.Dispose();
                this.tracer = null;
            }

            base.Dispose(disposing);
        }

        private void Start()
        {
            if (this.serviceStopped != null)
            {
                return;
            }

            this.serviceStopped = new ManualResetEvent(false);
            this.serviceThread = new Thread(this.Run);

            this.serviceThread.Start();
        }

        /// <summary>
        /// To work around a behavior in ProjFS where notification masks on files that have been opened in virtualization instance are not invalidated
        /// when the virtualization instance is restarted, GSD waits until after there has been a reboot before enabling the GitStatusCache.
        /// GSD.Service signals that there has been a reboot since installing a version of GSD that supports the GitStatusCache via
        /// the existence of the file "EnableGitStatusCacheToken.dat" in {CommonApplicationData}\GSD\GSD.Service
        /// (i.e. ProgramData\GSD\GSD.Service\EnableGitStatusCacheToken.dat on Windows).
        /// </summary>
        private void CheckEnableGitStatusCacheTokenFile()
        {
            try
            {
                string statusCacheVersionTokenPath = Path.Combine(GSDPlatform.Instance.GetDataRootForGSDComponent(GSDConstants.Service.ServiceName), GSDConstants.GitStatusCache.EnableGitStatusCacheTokenFile);
                if (File.Exists(statusCacheVersionTokenPath))
                {
                    this.tracer.RelatedInfo($"CheckEnableGitStatusCache: EnableGitStatusCacheToken file already exists at {statusCacheVersionTokenPath}.");
                    return;
                }

                DateTime lastRebootTime = NativeMethods.GetLastRebootTime();

                // GitStatusCache was included with GSD on disk version 16. The 1st time GSD that is at or above on disk version
                // is installed, it will write out a file indicating that the installation is "OnDiskVersion16Capable".
                // We can query the properties of this file to get the installation time, and compare this with the last reboot time for
                // this machine.
                string fileToCheck = Path.Combine(Configuration.AssemblyPath, GSDConstants.InstallationCapabilityFiles.OnDiskVersion16CapableInstallation);

                if (File.Exists(fileToCheck))
                {
                    DateTime installTime = File.GetCreationTime(fileToCheck);
                    if (lastRebootTime > installTime)
                    {
                        this.tracer.RelatedInfo($"CheckEnableGitStatusCache: Writing out EnableGitStatusCacheToken file. GSD installation time: {installTime}, last Reboot time: {lastRebootTime}.");
                        File.WriteAllText(statusCacheVersionTokenPath, string.Empty);
                    }
                    else
                    {
                        this.tracer.RelatedInfo($"CheckEnableGitStatusCache: Not writing EnableGitStatusCacheToken file - machine has not been rebooted since OnDiskVersion16Capable installation. GSD installation time: {installTime}, last reboot time: {lastRebootTime}");
                    }
                }
                else
                {
                    this.tracer.RelatedError($"Unable to determine GSD installation time: {fileToCheck} does not exist.");
                }
            }
            catch (Exception ex)
            {
                // Do not crash the service if there is an error here. Service is still healthy, but we
                // might not create file indicating that it is OK to use GitStatusCache.
                this.tracer.RelatedError($"{nameof(this.CheckEnableGitStatusCacheTokenFile)}: Unable to determine GSD installation time or write EnableGitStatusCacheToken file due to exception. Exception: {ex.ToString()}");
            }
        }

        private void LogExceptionAndExit(Exception e, string method)
        {
            EventMetadata metadata = new EventMetadata();
            metadata.Add("Area", EtwArea);
            metadata.Add("Exception", e.ToString());
            this.tracer.RelatedError(metadata, "Unhandled exception in " + method);
            Environment.Exit((int)ReturnCode.GenericError);
        }

        private void CreateServiceLogsDirectory(string serviceLogsDirectoryPath)
        {
            if (!Directory.Exists(serviceLogsDirectoryPath))
            {
                DirectorySecurity serviceDataRootSecurity = this.GetServiceDirectorySecurity(serviceLogsDirectoryPath);
                Directory.CreateDirectory(serviceLogsDirectoryPath);
            }
        }

        private void CreateAndConfigureProgramDataDirectories()
        {
            string serviceDataRootPath = Path.GetDirectoryName(this.serviceDataLocation);

            DirectorySecurity serviceDataRootSecurity = this.GetServiceDirectorySecurity(serviceDataRootPath);

            // Create GSD.Service and GSD.Upgrade related directories (if they don't already exist)
            Directory.CreateDirectory(serviceDataRootPath, serviceDataRootSecurity);
            Directory.CreateDirectory(this.serviceDataLocation, serviceDataRootSecurity);
            Directory.CreateDirectory(ProductUpgraderInfo.GetUpgradeProtectedDataDirectory(), serviceDataRootSecurity);

            // Ensure the ACLs are set correctly on any files or directories that were already created (e.g. after upgrading VFS4G)
            Directory.SetAccessControl(serviceDataRootPath, serviceDataRootSecurity);

            // Special rules for the upgrader logs, as non-elevated users need to be be able to write
            this.CreateAndConfigureUpgradeLogDirectory();
        }

        private void CreateAndConfigureUpgradeLogDirectory()
        {
            string upgradeLogsPath = ProductUpgraderInfo.GetLogDirectoryPath();

            string error;
            if (!GSDPlatform.Instance.FileSystem.TryCreateDirectoryWithAdminAndUserModifyPermissions(upgradeLogsPath, out error))
            {
                EventMetadata metadata = new EventMetadata();
                metadata.Add("Area", EtwArea);
                metadata.Add(nameof(upgradeLogsPath), upgradeLogsPath);
                metadata.Add(nameof(error), error);
                this.tracer.RelatedWarning(
                    metadata,
                    $"{nameof(this.CreateAndConfigureUpgradeLogDirectory)}: Failed to create upgrade logs directory",
                    Keywords.Telemetry);
            }
        }

        private DirectorySecurity GetServiceDirectorySecurity(string serviceDataRootPath)
        {
            DirectorySecurity serviceDataRootSecurity;
            if (Directory.Exists(serviceDataRootPath))
            {
                this.tracer.RelatedInfo($"{nameof(this.GetServiceDirectorySecurity)}: {serviceDataRootPath} exists, modifying ACLs.");
                serviceDataRootSecurity = Directory.GetAccessControl(serviceDataRootPath);
            }
            else
            {
                this.tracer.RelatedInfo($"{nameof(this.GetServiceDirectorySecurity)}: {serviceDataRootPath} does not exist, creating new ACLs.");
                serviceDataRootSecurity = new DirectorySecurity();
            }

            // Protect the access rules from inheritance and remove any inherited rules
            serviceDataRootSecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            // Remove any existing ACLs and add new ACLs for users and admins
            WindowsFileSystem.RemoveAllFileSystemAccessRulesFromDirectorySecurity(serviceDataRootSecurity);
            WindowsFileSystem.AddUsersAccessRulesToDirectorySecurity(serviceDataRootSecurity, grantUsersModifyPermissions: false);
            WindowsFileSystem.AddAdminAccessRulesToDirectorySecurity(serviceDataRootSecurity);

            return serviceDataRootSecurity;
        }
    }
}
