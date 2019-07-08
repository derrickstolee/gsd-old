using System;
using System.IO;
using System.Linq;

namespace GSD.Common
{
    public static class Paths
    {
        public static string GetGitEnlistmentRoot(string directory)
        {
            return GetRoot(directory, GSDConstants.DotGit.Root);
        }

        public static string GetRoot(string startingDirectory, string rootName)
        {
            startingDirectory = startingDirectory.TrimEnd(Path.DirectorySeparatorChar);
            DirectoryInfo dirInfo;

            try
            {
                dirInfo = new DirectoryInfo(startingDirectory);
            }
            catch (Exception)
            {
                return null;
            }

            while (dirInfo != null)
            {
                if (dirInfo.Exists)
                {
                    DirectoryInfo[] dotGSDDirs = new DirectoryInfo[0];

                    try
                    {
                        dotGSDDirs = dirInfo.GetDirectories(rootName);
                    }
                    catch (IOException)
                    {
                    }

                    if (dotGSDDirs.Count() == 1)
                    {
                        return dirInfo.FullName;
                    }
                }

                dirInfo = dirInfo.Parent;
            }

            return null;
        }

        public static string ConvertPathToGitFormat(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, GSDConstants.GitPathSeparator);
        }
    }
}
