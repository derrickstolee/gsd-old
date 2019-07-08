using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;

namespace GSD.PreBuild
{
    public class GenerateGSDInstallersNuspec : Task
    {
        [Required]
        public string GSDSetupPath { get; set; }

        [Required]
        public string GitPackageVersion { get; set; }

        [Required]
        public string PackagesPath { get; set; }

        [Required]
        public string OutputFile { get; set; }

        public override bool Execute()
        {
            this.Log.LogMessage(MessageImportance.High, "Generating GSD.Installers.nuspec");

            this.GSDSetupPath = Path.GetFullPath(this.GSDSetupPath);
            this.PackagesPath = Path.GetFullPath(this.PackagesPath);

            File.WriteAllText(
                this.OutputFile,
                string.Format(
@"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>GSD.Installers</id>
    <version>$GSDVersion$</version>
    <authors>Microsoft</authors>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>GSD and G4W installers</description>
  </metadata>
  <files>
    <file src=""{0}"" target=""GSD"" />
    <file src=""{1}\GitForWindows.GVFS.Installer.{2}\tools\*"" target=""G4W"" />
    <file src=""{1}\GitForWindows.GVFS.Portable.{2}\tools\*"" target=""G4W"" />
  </files>
</package>",
                    this.GSDSetupPath,
                    this.PackagesPath,
                    this.GitPackageVersion));

            return true;
        }
    }
}
