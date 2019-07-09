#!/bin/bash
. "$(dirname ${BASH_SOURCE[0]})/InitializeEnvironment.sh"

REPOURL=$1

CONFIGURATION=$2
if [ -z $CONFIGURATION ]; then
  CONFIGURATION=Debug
fi

$VFS_PUBLISHDIR/gvfs clone $REPOURL ~/GSDTest --local-cache-path ~/GSDTest/.gvfsCache --no-mount --no-prefetch
