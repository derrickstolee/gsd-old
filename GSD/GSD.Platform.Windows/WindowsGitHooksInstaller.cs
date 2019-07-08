using GSD.Common;
using GSD.Common.FileSystem;
using GSD.Common.Git;
using System;
using System.IO;

namespace GSD.Platform.Windows
{
    internal static class WindowsGitHooksInstaller
    {
        private const string HooksConfigContentTemplate =
@"########################################################################
#   Automatically generated file, do not modify.
#   See {0} config setting
########################################################################
{1}";

        public static void CreateHookCommandConfig(GSDContext context, string hookName, string commandHookPath)
        {
            string targetPath = commandHookPath + GSDConstants.GitConfig.HooksExtension;

            try
            {
                string configSetting = GSDConstants.GitConfig.HooksPrefix + hookName;
                string mergedHooks = MergeHooks(context, configSetting, hookName);

                string contents = string.Format(HooksConfigContentTemplate, configSetting, mergedHooks);
                Exception ex;
                if (!context.FileSystem.TryWriteTempFileAndRename(targetPath, contents, out ex))
                {
                    throw new RetryableException("Error installing " + targetPath, ex);
                }
            }
            catch (IOException io)
            {
                throw new RetryableException("Error installing " + targetPath, io);
            }
        }

        private static string MergeHooks(GSDContext context, string configSettingName, string hookName)
        {
            GitProcess configProcess = new GitProcess(context.Enlistment);
            string filename;
            string[] defaultHooksLines = { };

            if (configProcess.TryGetFromConfig(configSettingName, forceOutsideEnlistment: true, value: out filename) && filename != null)
            {
                filename = filename.Trim(' ', '\n');
                defaultHooksLines = File.ReadAllLines(filename);
            }

            return HooksInstaller.MergeHooksData(defaultHooksLines, filename, hookName);
        }
    }
}
