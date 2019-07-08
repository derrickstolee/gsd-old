. "$(dirname ${BASH_SOURCE[0]})/InitializeEnvironment.sh"

if [ -z $1 ]; then
  echo "Version Number not defined for CommonAssemblyVersion.cs"
fi

# Update the version number in GSD.props for other consumers of GSDVersion
sed -i "" -E "s@<GSDVersion>[0-9]+(\.[0-9]+)*</GSDVersion>@<GSDVersion>$1</GSDVersion>@g" $VFS_SRCDIR/GSD/GSD.Build/GSD.props

# Then generate CommonAssemblyVersion.cs
cat >$VFS_OUTPUTDIR/CommonAssemblyVersion.cs <<TEMPLATE
using System.Reflection;

[assembly: AssemblyVersion("$1")]
[assembly: AssemblyFileVersion("$1")]
TEMPLATE
