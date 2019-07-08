. "$(dirname ${BASH_SOURCE[0]})/InitializeEnvironment.sh"

pkill -9 -l GSD.FunctionalTests
pkill -9 -l git
pkill -9 -l gvfs
pkill -9 -l GSD.Mount
pkill -9 -l prjfs-log

$VFS_SRCDIR/ProjFS.Mac/Scripts/UnloadPrjFSKext.sh

if [ -d /GSD.FT ]; then
    sudo rm -r /GSD.FT
fi
