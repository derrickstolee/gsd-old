using GSD.Common;
using GSD.Common.Tracing;
using GSD.Platform.Windows;
using GSD.Service.Handlers;

namespace GSD.Service
{
    public class GSDMountProcess : IRepoMounter
    {
        private readonly ITracer tracer;

        public GSDMountProcess(ITracer tracer)
        {
            this.tracer = tracer;
        }

        public bool MountRepository(string repoRoot, int sessionId)
        {
            using (CurrentUser currentUser = new CurrentUser(this.tracer, sessionId))
            {
                if (!this.CallGSDMount(repoRoot, currentUser))
                {
                    this.tracer.RelatedError($"{nameof(this.MountRepository)}: Unable to start the GSD.exe process.");
                    return false;
                }

                string errorMessage;
                if (!GSDEnlistment.WaitUntilMounted(this.tracer, repoRoot, false, out errorMessage))
                {
                    this.tracer.RelatedError(errorMessage);
                    return false;
                }
            }

            return true;
        }

        private bool CallGSDMount(string repoRoot, CurrentUser currentUser)
        {
            InternalVerbParameters mountInternal = new InternalVerbParameters(startedByService: true);
            return currentUser.RunAs(
                Configuration.Instance.GSDLocation,
                $"mount {repoRoot} --{GSDConstants.VerbParameters.InternalUseOnly} {mountInternal.ToJson()}");
        }
    }
}
