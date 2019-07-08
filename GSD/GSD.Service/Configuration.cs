using GSD.Common;
using System.IO;

namespace GSD.Service
{
    public class Configuration
    {
        private static Configuration instance = new Configuration();
        private static string assemblyPath = null;

        private Configuration()
        {
            this.GSDLocation = Path.Combine(AssemblyPath, GSDPlatform.Instance.Constants.GSDExecutableName);
            this.GSDServiceUILocation = Path.Combine(AssemblyPath, GSDConstants.Service.UIName + GSDPlatform.Instance.Constants.ExecutableExtension);
        }

        public static Configuration Instance
        {
            get
            {
                return instance;
            }
        }

        public static string AssemblyPath
        {
            get
            {
                if (assemblyPath == null)
                {
                    assemblyPath = ProcessHelper.GetCurrentProcessLocation();
                }

                return assemblyPath;
            }
        }

        public string GSDLocation { get; private set; }
        public string GSDServiceUILocation { get; private set; }
    }
}
