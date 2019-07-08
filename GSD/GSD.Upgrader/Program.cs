using CommandLine;
using GSD.PlatformLoader;

namespace GSD.Upgrader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GSDPlatformLoader.Initialize();

            Parser.Default.ParseArguments<UpgradeOptions>(args)
                .WithParsed(options =>  UpgradeOrchestratorFactory.Create(options).Execute());
        }
    }
}
