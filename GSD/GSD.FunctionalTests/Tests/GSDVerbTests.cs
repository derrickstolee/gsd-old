using GSD.Tests.Should;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;

namespace GSD.FunctionalTests.Tests
{
    [TestFixture]
    public class GSDVerbTests
    {
        public GSDVerbTests()
        {
        }

        private enum ExpectedReturnCode
        {
            Success = 0,
            ParsingError = 1,
        }

        [TestCase]
        public void UnknownVerb()
        {
            this.CallGSD("help", ExpectedReturnCode.Success);
            this.CallGSD("unknownverb", ExpectedReturnCode.ParsingError);
        }

        [TestCase]
        public void UnknownArgs()
        {
            this.CallGSD("log --help", ExpectedReturnCode.Success);
            this.CallGSD("log --unknown-arg", ExpectedReturnCode.ParsingError);
        }

        private void CallGSD(string args, ExpectedReturnCode expectedErrorCode)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo(GSDTestConfig.PathToGSD);
            processInfo.Arguments = args;
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;

            using (Process process = Process.Start(processInfo))
            {
                string result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                process.ExitCode.ShouldEqual((int)expectedErrorCode, result);
            }
        }
    }
}
