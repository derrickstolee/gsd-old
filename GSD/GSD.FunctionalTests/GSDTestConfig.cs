using System.IO;

namespace GSD.FunctionalTests
{
    public static class GSDTestConfig
    {
        public static string RepoToClone { get; set; }

        public static bool NoSharedCache { get; set; }

        public static string LocalCacheRoot { get; set; }

        public static object[] FileSystemRunners { get; set; }

        public static object[] GitRepoTestsValidateWorkTree { get; set; }

        public static bool TestGSDOnPath { get; set; }

        public static bool ReplaceInboxProjFS { get; set; }

        public static string PathToGSD
        {
            get
            {
                return
                    TestGSDOnPath ?
                    Properties.Settings.Default.PathToGSD :
                    Path.Combine(Properties.Settings.Default.CurrentDirectory, Properties.Settings.Default.PathToGSD);
            }
        }

        public static string DotGSDRoot { get; set; }
    }
}
