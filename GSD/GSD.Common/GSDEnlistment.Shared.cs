using GSD.Common.Tracing;
using System;
using System.Security;

namespace GSD.Common
{
    public partial class GSDEnlistment
    {
        public static bool IsUnattended(ITracer tracer)
        {
            try
            {
                return Environment.GetEnvironmentVariable(GSDConstants.UnattendedEnvironmentVariable) == "1";
            }
            catch (SecurityException e)
            {
                if (tracer != null)
                {
                    EventMetadata metadata = new EventMetadata();
                    metadata.Add("Area", nameof(GSDEnlistment));
                    metadata.Add("Exception", e.ToString());
                    tracer.RelatedError(metadata, "Unable to read environment variable " + GSDConstants.UnattendedEnvironmentVariable);
                }

                return false;
            }
        }
    }
}
