. "$(dirname ${BASH_SOURCE[0]})/InitializeEnvironment.sh"

GSDPROPS=$VFS_SRCDIR/GSD/GSD.Build/GSD.props
GITVERSION="$(cat $GSDPROPS | grep GitPackageVersion | grep -Eo '[0-9.]+(-\w+)?')"
echo $GITVERSION