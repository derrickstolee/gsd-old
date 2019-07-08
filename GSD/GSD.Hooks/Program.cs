using GSD.Common;
using GSD.Common.NamedPipes;
using GSD.Hooks.HooksPlatform;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GSD.Hooks
{
    public class Program
    {
        private const string PreCommandHook = "pre-command";
        private const string PostCommandHook = "post-command";

        private const string GitPidArg = "--git-pid=";
        private const int InvalidProcessId = -1;

        private const int PostCommandSpinnerDelayMs = 500;

        private static Dictionary<string, string> specialArgValues = new Dictionary<string, string>();
        private static string enlistmentRoot;
        private static string enlistmentPipename;
        private static Random random = new Random();

        private delegate void LockRequestDelegate(bool unattended, string[] args, int pid, NamedPipeClient pipeClient);

        public static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2)
                {
                    ExitWithError("Usage: gvfs.hooks.exe --git-pid=<pid> <hook> <git verb> [<other arguments>]");
                }

                bool unattended = GSDEnlistment.IsUnattended(tracer: null);

                string errorMessage;
                string normalizedCurrentDirectory;
                if (!GSDHooksPlatform.TryGetNormalizedPath(Environment.CurrentDirectory, out normalizedCurrentDirectory, out errorMessage))
                {
                    ExitWithError($"Failed to determine final path for current directory {Environment.CurrentDirectory}. Error: {errorMessage}");
                }

                if (!GSDHooksPlatform.TryGetGSDEnlistmentRoot(Environment.CurrentDirectory, out enlistmentRoot, out errorMessage))
                {
                    // Nothing to hook when being run outside of a GSD repo.
                    // This is also the path when run with --git-dir outside of a GSD directory, see Story #949665
                    Environment.Exit(0);
                }

                enlistmentPipename = GSDHooksPlatform.GetNamedPipeName(enlistmentRoot);

                switch (GetHookType(args))
                {
                    case PreCommandHook:
                        CheckForLegalCommands(args);
                        RunPreCommands(args);
                        break;

                    case PostCommandHook:
                        RunPostCommands(args, unattended);
                        break;

                    default:
                        ExitWithError("Unrecognized hook: " + string.Join(" ", args));
                        break;
                }
            }
            catch (Exception ex)
            {
                ExitWithError("Unexpected exception: " + ex.ToString());
            }
        }

        private static void RunPreCommands(string[] args)
        {
            string command = GetGitCommand(args);
            switch (command)
            {
                case "fetch":
                case "pull":
                    ProcessHelper.Run("gvfs", "prefetch --commits", redirectOutput: false);
                    break;
            }
        }

        private static void RunPostCommands(string[] args, bool unattended)
        {
            if (!unattended)
            {
                RemindUpgradeAvailable();
            }
        }

        private static void RemindUpgradeAvailable()
        {
            // The idea is to generate a random number between 0 and 100. To make
            // sure that the reminder is displayed only 10% of the times a git
            // command is run, check that the random number is between 0 and 10,
            // which will have a probability of 10/100 == 10%.
            int reminderFrequency = 10;
            int randomValue = random.Next(0, 100);

            if (randomValue <= reminderFrequency &&
                ProductUpgraderInfo.IsLocalUpgradeAvailable(tracer: null, highestAvailableVersionDirectory: GSDHooksPlatform.GetUpgradeHighestAvailableVersionDirectory()))
            {
                Console.WriteLine(Environment.NewLine + GSDConstants.UpgradeVerbMessages.ReminderNotification);
            }
        }

        private static void ExitWithError(params string[] messages)
        {
            foreach (string message in messages)
            {
                Console.WriteLine(message);
            }

            Environment.Exit(1);
        }

        private static void CheckForLegalCommands(string[] args)
        {
            string command = GetGitCommand(args);
            switch (command)
            {
                case "gui":
                    ExitWithError("To access the 'git gui' in a GSD repo, please invoke 'git-gui.exe' instead.");
                    break;
            }
        }

        private static void RunLockRequest(string[] args, bool unattended, LockRequestDelegate requestToRun)
        {
            try
            {
                if (ShouldLock(args))
                {
                    using (NamedPipeClient pipeClient = new NamedPipeClient(enlistmentPipename))
                    {
                        if (!pipeClient.Connect())
                        {
                            ExitWithError("The repo does not appear to be mounted. Use 'gvfs status' to check.");
                        }

                        int pid = GetParentPid(args);
                        if (pid == Program.InvalidProcessId ||
                            !GSDHooksPlatform.IsProcessActive(pid))
                        {
                            ExitWithError("GSD.Hooks: Unable to find parent git.exe process " + "(PID: " + pid + ").");
                        }

                        requestToRun(unattended, args, pid, pipeClient);
                    }
                }
            }
            catch (Exception exc)
            {
                ExitWithError(
                    "Unable to initialize Git command.",
                    "Ensure that GSD is running.",
                    exc.ToString());
            }
        }

        private static string GenerateFullCommand(string[] args)
        {
            return "git " + string.Join(" ", args.Skip(1).Where(arg => !arg.StartsWith(GitPidArg)));
        }

        private static int GetParentPid(string[] args)
        {
            string pidArg = args.SingleOrDefault(x => x.StartsWith(GitPidArg));
            if (!string.IsNullOrEmpty(pidArg))
            {
                pidArg = pidArg.Remove(0, GitPidArg.Length);
                int pid;
                if (int.TryParse(pidArg, out pid))
                {
                    return pid;
                }
            }

            ExitWithError(
                "Git did not supply the process Id.",
                "Ensure you are using the correct version of the git client.");

            return Program.InvalidProcessId;
        }

        private static string BuildUpdatePlaceholderFailureMessage(List<string> fileList, string failedOperation, string recoveryCommand)
        {
            if (fileList == null || fileList.Count == 0)
            {
                return string.Empty;
            }

            fileList.Sort(StringComparer.OrdinalIgnoreCase);
            string message = "\nGSD was unable to " + failedOperation + " the following files. To recover, close all handles to the files and run these commands:";
            message += string.Concat(fileList.Select(x => "\n    " + recoveryCommand + x));
            return message;
        }

        private static bool TryRemoveArg(ref string[] args, string argName, out string output)
        {
            output = null;
            int argIdx = Array.IndexOf(args, argName);
            if (argIdx >= 0)
            {
                if (argIdx + 1 < args.Length)
                {
                    output = args[argIdx + 1];
                    args = args.Take(argIdx).Concat(args.Skip(argIdx + 2)).ToArray();
                    return true;
                }
                else
                {
                    ExitWithError("Missing value for {0}.", argName);
                }
            }

            return false;
        }

        private static bool IsGitEnvVarDisabled(string envVar)
        {
            string envVarValue = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrEmpty(envVarValue))
            {
                if (string.Equals(envVarValue, "false", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(envVarValue, "no", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(envVarValue, "off", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(envVarValue, "0", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldLock(string[] args)
        {
            string gitCommand = GetGitCommand(args);

            switch (gitCommand)
            {
                // Keep these alphabetically sorted
                case "blame":
                case "branch":
                case "cat-file":
                case "check-attr":
                case "check-ignore":
                case "check-mailmap":
                case "commit-graph":
                case "config":
                case "credential":
                case "diff":
                case "diff-files":
                case "diff-index":
                case "diff-tree":
                case "difftool":
                case "fetch":
                case "for-each-ref":
                case "help":
                case "hash-object":
                case "index-pack":
                case "log":
                case "ls-files":
                case "ls-tree":
                case "merge-base":
                case "multi-pack-index":
                case "name-rev":
                case "push":
                case "remote":
                case "rev-list":
                case "rev-parse":
                case "show":
                case "show-ref":
                case "symbolic-ref":
                case "tag":
                case "unpack-objects":
                case "update-ref":
                case "version":
                case "web--browse":
                    return false;

                /*
                 * There are several git commands that are "unsupoorted" in virtualized (VFS4G)
                 * enlistments that are blocked by git. Usually, these are blocked before they acquire
                 * a GSDLock, but the submodule command is different, and is blocked after acquiring the
                 * GSD lock. This can cause issues if another action is attempting to create placeholders.
                 * As we know the submodule command is a no-op, allow it to proceed without acquiring the
                 * GSDLock. I have filed issue #1164 to track having git block all unsupported commands
                 * before calling the pre-command hook.
                 */
                case "submodule":
                    return false;
            }

            if (gitCommand == "reset" && args.Contains("--soft"))
            {
                return false;
            }

            if (!KnownGitCommands.Contains(gitCommand) &&
                IsAlias(gitCommand))
            {
                return false;
            }

            return true;
        }

        private static bool ContainsArg(string[] actualArgs, string expectedArg)
        {
            return actualArgs.Contains(expectedArg, StringComparer.OrdinalIgnoreCase);
        }

        private static string GetHookType(string[] args)
        {
            return args[0].ToLowerInvariant();
        }

        private static string GetGitCommand(string[] args)
        {
            string command = args[1].ToLowerInvariant();
            if (command.StartsWith("git-"))
            {
                command = command.Substring(4);
            }

            return command;
        }

        private static bool IsAlias(string command)
        {
            ProcessResult result = ProcessHelper.Run("git", "config --get alias." + command);

            return !string.IsNullOrEmpty(result.Output);
        }

        private static string GetGitCommandSessionId()
        {
            try
            {
                return Environment.GetEnvironmentVariable("GIT_TR2_PARENT_SID", EnvironmentVariableTarget.Process) ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
