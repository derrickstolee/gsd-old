using CommandLine;
using GSD.PlatformLoader;
using System;

namespace GSD.Mount
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GSDPlatformLoader.Initialize();
            try
            {
                Parser.Default.ParseArguments<InProcessMountVerb>(args)
                    .WithParsed(mount => mount.Execute());
            }
            catch (MountAbortedException e)
            {
                // Calling Environment.Exit() is required, to force all background threads to exit as well
                Environment.Exit((int)e.Verb.ReturnCode);
            }
        }
    }
}
