using CommandLine;
using GSD.Common;
using GSD.Common.NamedPipes;
using System.Diagnostics;

namespace GSD.CommandLine
{
    [Verb(UnmountVerb.UnmountVerbName, HelpText = "Unmount a GSD virtual repo")]
    public class UnmountVerb : GSDVerb
    {
        private const string UnmountVerbName = "unmount";

        [Value(
            0,
            Required = false,
            Default = "",
            MetaName = "Enlistment Root Path",
            HelpText = "Full or relative path to the GSD enlistment root")]
        public override string EnlistmentRootPathParameter { get; set; }

        [Option(
            GSDConstants.VerbParameters.Unmount.SkipLock,
            Default = false,
            Required = false,
            HelpText = "Force unmount even if the lock is not available.")]
        public bool SkipLock { get; set; }

        public bool SkipUnregister { get; set; }

        protected override string VerbName
        {
            get { return UnmountVerbName; }
        }

        public override void Execute()
        {
            this.ValidatePathParameter(this.EnlistmentRootPathParameter);

            string errorMessage;
            string root;
            if (!GSDPlatform.Instance.TryGetGSDEnlistmentRoot(this.EnlistmentRootPathParameter, out root, out errorMessage))
            {
                this.ReportErrorAndExit(
                   "Error: '{0}' is not a valid GSD enlistment",
                   this.EnlistmentRootPathParameter);
            }

            if (!this.SkipLock)
            {
                this.AcquireLock(root);
            }

            if (!this.ShowStatusWhileRunning(
                () => { return this.Unmount(root, out errorMessage); },
                "Unmounting"))
            {
                this.ReportErrorAndExit(errorMessage);
            }

            if (!this.Unattended && !this.SkipUnregister)
            {
                if (!this.ShowStatusWhileRunning(
                    () => { return this.UnregisterRepo(root, out errorMessage); },
                    "Unregistering automount"))
                {
                    this.Output.WriteLine("    WARNING: " + errorMessage);
                }
            }
        }

        private bool Unmount(string enlistmentRoot, out string errorMessage)
        {
            errorMessage = string.Empty;

            string pipeName = GSDPlatform.Instance.GetNamedPipeName(enlistmentRoot);
            string rawGetStatusResponse = string.Empty;

            try
            {
                using (NamedPipeClient pipeClient = new NamedPipeClient(pipeName))
                {
                    if (!pipeClient.Connect())
                    {
                        errorMessage = "Unable to connect to GSD.Mount";
                        return false;
                    }

                    pipeClient.SendRequest(NamedPipeMessages.GetStatus.Request);
                    rawGetStatusResponse = pipeClient.ReadRawResponse();
                    NamedPipeMessages.GetStatus.Response getStatusResponse =
                        NamedPipeMessages.GetStatus.Response.FromJson(rawGetStatusResponse);

                    switch (getStatusResponse.MountStatus)
                    {
                        case NamedPipeMessages.GetStatus.Mounting:
                            errorMessage = "Still mounting, please try again later";
                            return false;

                        case NamedPipeMessages.GetStatus.Unmounting:
                            errorMessage = "Already unmounting, please wait";
                            return false;

                        case NamedPipeMessages.GetStatus.Ready:
                            break;

                        case NamedPipeMessages.GetStatus.MountFailed:
                            break;

                        default:
                            errorMessage = "Unrecognized response to GetStatus: " + rawGetStatusResponse;
                            return false;
                    }

                    pipeClient.SendRequest(NamedPipeMessages.Unmount.Request);
                    string unmountResponse = pipeClient.ReadRawResponse();

                    switch (unmountResponse)
                    {
                        case NamedPipeMessages.Unmount.Acknowledged:
                            string finalResponse = pipeClient.ReadRawResponse();
                            if (finalResponse == NamedPipeMessages.Unmount.Completed)
                            {
                                errorMessage = string.Empty;
                                return true;
                            }
                            else
                            {
                                errorMessage = "Unrecognized final response to unmount: " + finalResponse;
                                return false;
                            }

                        case NamedPipeMessages.Unmount.NotMounted:
                            errorMessage = "Unable to unmount, repo was not mounted";
                            return false;

                        case NamedPipeMessages.Unmount.MountFailed:
                            errorMessage = "Unable to unmount, previous mount attempt failed";
                            return false;

                        default:
                            errorMessage = "Unrecognized response to unmount: " + unmountResponse;
                            return false;
                    }
                }
            }
            catch (BrokenPipeException e)
            {
                errorMessage = "Unable to communicate with GSD: " + e.ToString();
                return false;
            }
        }

        private bool UnregisterRepo(string rootPath, out string errorMessage)
        {
            errorMessage = string.Empty;
            NamedPipeMessages.UnregisterRepoRequest request = new NamedPipeMessages.UnregisterRepoRequest();
            request.EnlistmentRoot = rootPath;

            using (NamedPipeClient client = new NamedPipeClient(this.ServicePipeName))
            {
                if (!client.Connect())
                {
                    errorMessage = "Unable to unregister repo because GSD.Service is not responding. " + GSDVerb.StartServiceInstructions;
                    return false;
                }

                try
                {
                    client.SendRequest(request.ToMessage());
                    NamedPipeMessages.Message response = client.ReadResponse();
                    if (response.Header == NamedPipeMessages.UnregisterRepoRequest.Response.Header)
                    {
                        NamedPipeMessages.UnregisterRepoRequest.Response message = NamedPipeMessages.UnregisterRepoRequest.Response.FromMessage(response);

                        if (message.State != NamedPipeMessages.CompletionState.Success)
                        {
                            errorMessage = message.ErrorMessage;
                            return false;
                        }
                        else
                        {
                            errorMessage = string.Empty;
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

        private void AcquireLock(string enlistmentRoot)
        {
            string pipeName = GSDPlatform.Instance.GetNamedPipeName(enlistmentRoot);
            using (NamedPipeClient pipeClient = new NamedPipeClient(pipeName))
            {
                try
                {
                    if (!pipeClient.Connect())
                    {
                        this.ReportErrorAndExit("Unable to connect to GSD while acquiring lock to unmount.  Try 'gvfs status' to verify if the repo is mounted.");
                        return;
                    }

                    Process currentProcess = Process.GetCurrentProcess();
                    string result = null;
                    if (!GSDLock.TryAcquireGSDLockForProcess(
                            this.Unattended,
                            pipeClient,
                            "gvfs unmount",
                            currentProcess.Id,
                            GSDPlatform.Instance.IsElevated(),
                            isConsoleOutputRedirectedToFile: GSDPlatform.Instance.IsConsoleOutputRedirectedToFile(),
                            checkAvailabilityOnly: false,
                            gvfsEnlistmentRoot: enlistmentRoot,
                            gitCommandSessionId: string.Empty,
                            result: out result))
                    {
                        this.ReportErrorAndExit("Unable to acquire the lock prior to unmount. " + result);
                    }
                }
                catch (BrokenPipeException)
                {
                    this.ReportErrorAndExit("Unable to acquire the lock prior to unmount.  Try 'gvfs status' to verify if the repo is mounted.");
                }
            }
        }
    }
}
