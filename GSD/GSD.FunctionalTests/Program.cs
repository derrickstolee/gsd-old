using GSD.FunctionalTests.Tests;
using GSD.FunctionalTests.Tools;
using GSD.Tests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace GSD.FunctionalTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Properties.Settings.Default.Initialize();
            NUnitRunner runner = new NUnitRunner(args);

            if (runner.HasCustomArg("--no-shared-gvfs-cache"))
            {
                Console.WriteLine("Running without a shared git object cache");
                GSDTestConfig.NoSharedCache = true;
            }

            if (runner.HasCustomArg("--test-gsd-on-path"))
            {
                Console.WriteLine("Running tests against GSD on path");
                GSDTestConfig.TestGSDOnPath = true;
            }

            GSDTestConfig.LocalCacheRoot = runner.GetCustomArgWithParam("--shared-gvfs-cache-root");

            HashSet<string> includeCategories = new HashSet<string>();
            HashSet<string> excludeCategories = new HashSet<string>();

            if (runner.HasCustomArg("--full-suite"))
            {
                Console.WriteLine("Running the full suite of tests");

                GSDTestConfig.GitRepoTestsValidateWorkTree = DataSources.AllBools;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    GSDTestConfig.FileSystemRunners = FileSystemRunners.FileSystemRunner.AllWindowsRunners;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    GSDTestConfig.FileSystemRunners = FileSystemRunners.FileSystemRunner.AllMacRunners;
                }
            }
            else
            {
                GSDTestConfig.GitRepoTestsValidateWorkTree =
                    new object[]
                    {
                        new object[] { true }
                    };

                if (runner.HasCustomArg("--extra-only"))
                {
                    Console.WriteLine("Running only the tests marked as ExtraCoverage");
                    includeCategories.Add(Categories.ExtraCoverage);
                }
                else
                {
                    excludeCategories.Add(Categories.ExtraCoverage);
                }

                GSDTestConfig.FileSystemRunners = FileSystemRunners.FileSystemRunner.DefaultRunners;
            }

            if (runner.HasCustomArg("--windows-only"))
            {
                includeCategories.Add(Categories.WindowsOnly);

                // RunTests unions all includeCategories.  Remove ExtraCoverage to
                // ensure that we only run tests flagged as WindowsOnly
                includeCategories.Remove(Categories.ExtraCoverage);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                excludeCategories.Add(Categories.MacTODO.FailsOnBuildAgent);
                excludeCategories.Add(Categories.MacTODO.NeedsNewFolderCreateNotification);
                excludeCategories.Add(Categories.MacTODO.NeedsGSDConfig);
                excludeCategories.Add(Categories.MacTODO.NeedsDehydrate);
                excludeCategories.Add(Categories.MacTODO.NeedsServiceVerb);
                excludeCategories.Add(Categories.MacTODO.NeedsStatusCache);
                excludeCategories.Add(Categories.MacTODO.NeedsCorruptObjectFix);
                excludeCategories.Add(Categories.MacTODO.TestNeedsToLockFile);
                excludeCategories.Add(Categories.WindowsOnly);
            }
            else
            {
                excludeCategories.Add(Categories.MacOnly);
            }

            GSDTestConfig.DotGSDRoot = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? ".GSD" : ".gvfs";

            GSDTestConfig.RepoToClone =
                runner.GetCustomArgWithParam("--repo-to-clone")
                ?? Properties.Settings.Default.RepoToClone;

            RunBeforeAnyTests();
            Environment.ExitCode = runner.RunTests(includeCategories, excludeCategories);
            RunAfterAllTests();

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Tests completed. Press Enter to exit.");
                Console.ReadLine();
            }
        }

        private static void RunBeforeAnyTests()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                GSDServiceProcess.InstallService();

                string statusCacheVersionTokenPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.Create),
                    "GSD",
                    "GSD.Service",
                    "EnableGitStatusCacheToken.dat");

                if (!File.Exists(statusCacheVersionTokenPath))
                {
                    File.WriteAllText(statusCacheVersionTokenPath, string.Empty);
                }
            }
        }

        private static void RunAfterAllTests()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string serviceLogFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "GSD",
                    GSDServiceProcess.TestServiceName,
                    "Logs");

                Console.WriteLine("GSD.Service logs at '{0}' attached below.\n\n", serviceLogFolder);
                foreach (string filename in TestResultsHelper.GetAllFilesInDirectory(serviceLogFolder))
                {
                    TestResultsHelper.OutputFileContents(filename);
                }

                GSDServiceProcess.UninstallService();
            }

            PrintTestCaseStats.PrintRunTimeStats();
        }
    }
}
