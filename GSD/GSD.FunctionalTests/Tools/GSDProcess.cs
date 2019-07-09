using GSD.Tests.Should;
using System.Diagnostics;

namespace GSD.FunctionalTests.Tools
{
    public class GSDProcess
    {
        private readonly string pathToGSD;
        private readonly string enlistmentRoot;
        private readonly string localCacheRoot;

        public GSDProcess(string pathToGSD, string enlistmentRoot, string localCacheRoot)
        {
            this.pathToGSD = pathToGSD;
            this.enlistmentRoot = enlistmentRoot;
            this.localCacheRoot = localCacheRoot;
        }

        public void Clone(string repositorySource, string branchToCheckout, bool skipPrefetch)
        {
            string args = string.Format(
                "clone \"{0}\" \"{1}\" --branch \"{2}\" --local-cache-path \"{3}\" {4}",
                repositorySource,
                this.enlistmentRoot,
                branchToCheckout,
                this.localCacheRoot,
                skipPrefetch ? "--no-prefetch" : string.Empty);
            this.CallGSD(args, failOnError: true);
        }

        public void Mount()
        {
            string output;
            this.TryMount(out output).ShouldEqual(true, "GSD did not mount: " + output);
            output.ShouldNotContain(ignoreCase: true, unexpectedSubstrings: "warning");
        }

        public bool TryMount(out string output)
        {
            this.IsEnlistmentMounted().ShouldEqual(false, "GSD is already mounted");
            output = this.CallGSD("mount \"" + this.enlistmentRoot + "\"");
            return this.IsEnlistmentMounted();
        }

        public string Prefetch(string args, bool failOnError, string standardInput = null)
        {
            return this.CallGSD("prefetch \"" + this.enlistmentRoot + "\" " + args, failOnError, standardInput: standardInput);
        }

        public void Repair(bool confirm)
        {
            string confirmArg = confirm ? "--confirm " : string.Empty;
            this.CallGSD(
                "repair " + confirmArg + "\"" + this.enlistmentRoot + "\"",
                failOnError: true);
        }

        public string LooseObjectStep()
        {
            return this.CallGSD(
                "dehydrate \"" + this.enlistmentRoot + "\"",
                failOnError: true,
                internalParameter: GSDHelpers.GetInternalParameter("\\\"LooseObjects\\\""));
        }

        public string PackfileMaintenanceStep(long? batchSize)
        {
            string sizeString = batchSize.HasValue ? $"\\\"{batchSize.Value}\\\"" : "null";
            string internalParameter = GSDHelpers.GetInternalParameter("\\\"PackfileMaintenance\\\"", sizeString);
            return this.CallGSD(
                "dehydrate \"" + this.enlistmentRoot + "\"",
                failOnError: true,
                internalParameter: internalParameter);
        }

        public string PostFetchStep()
        {
            string internalParameter = GSDHelpers.GetInternalParameter("\\\"PostFetch\\\"");
            return this.CallGSD(
                "dehydrate \"" + this.enlistmentRoot + "\"",
                failOnError: true,
                internalParameter: internalParameter);
        }

        public string Diagnose()
        {
            return this.CallGSD("diagnose \"" + this.enlistmentRoot + "\"");
        }

        public string Status(string trace = null)
        {
            return this.CallGSD("status " + this.enlistmentRoot, trace: trace);
        }

        public string CacheServer(string args)
        {
            return this.CallGSD("cache-server " + args + " \"" + this.enlistmentRoot + "\"");
        }

        public void Unmount()
        {
            if (this.IsEnlistmentMounted())
            {
                string result = this.CallGSD("unmount \"" + this.enlistmentRoot + "\"", failOnError: true);
                this.IsEnlistmentMounted().ShouldEqual(false, "GSD did not unmount: " + result);
            }
        }

        public bool IsEnlistmentMounted()
        {
            string statusResult = this.CallGSD("status \"" + this.enlistmentRoot + "\"");
            return statusResult.Contains("Mount status: Ready");
        }

        public string RunServiceVerb(string argument)
        {
            return this.CallGSD("service " + argument, failOnError: true);
        }

        public string ReadConfig(string key, bool failOnError)
        {
            return this.CallGSD($"config {key}", failOnError).TrimEnd('\r', '\n');
        }

        public void WriteConfig(string key, string value)
        {
            this.CallGSD($"config {key} {value}", failOnError: true);
        }

        public void DeleteConfig(string key)
        {
            this.CallGSD($"config --delete {key}", failOnError: true);
        }

        private string CallGSD(string args, bool failOnError = false, string trace = null, string standardInput = null, string internalParameter = null)
        {
            ProcessStartInfo processInfo = null;
            processInfo = new ProcessStartInfo(this.pathToGSD);

            if (internalParameter == null)
            {
                internalParameter = GSDHelpers.GetInternalParameter();
            }

            processInfo.Arguments = args + " " + TestConstants.InternalUseOnlyFlag + " " + internalParameter;

            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;
            if (standardInput != null)
            {
                processInfo.RedirectStandardInput = true;
            }

            if (trace != null)
            {
                processInfo.EnvironmentVariables["GIT_TRACE"] = trace;
            }

            using (Process process = Process.Start(processInfo))
            {
                if (standardInput != null)
                {
                    process.StandardInput.Write(standardInput);
                    process.StandardInput.Close();
                }

                string result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (failOnError)
                {
                    process.ExitCode.ShouldEqual(0, $"Output: {result}\nError: {process.StandardError.ReadToEnd()}");
                }

                return result;
            }
        }
    }
}
