#!/bin/bash

. "$(dirname ${BASH_SOURCE[0]})/InitializeEnvironment.sh"

CONFIGURATION=$1
if [ -z $CONFIGURATION ]; then
  CONFIGURATION=Debug
fi

VERSION=$2
if [ -z $VERSION ]; then
  VERSION="0.2.173.2"
fi

if [ ! -d $VFS_OUTPUTDIR ]; then
  mkdir $VFS_OUTPUTDIR
fi

# Create the directory where we'll do pre build tasks
BUILDDIR=$VFS_OUTPUTDIR/GSD.Build
if [ ! -d $BUILDDIR ]; then
  mkdir $BUILDDIR || exit 1
fi

echo 'Downloading a VFS-enabled version of Git...'
$VFS_SCRIPTDIR/DownloadGSDGit.sh || exit 1
GITVERSION="$($VFS_SCRIPTDIR/GetGitVersionNumber.sh)"
GITPATH="$(find $VFS_PACKAGESDIR/gitformac.gvfs.installer/$GITVERSION -type f -name *.dmg)" || exit 1
echo "Downloaded Git $GITVERSION"
# Now that we have a path containing the version number, generate GSDConstants.GitVersion.cs
$VFS_SCRIPTDIR/GenerateGitVersionConstants.sh "$GITPATH" $BUILDDIR || exit 1

# If we're building the Profiling(Release) configuration, remove Profiling() for building .NET code
if [ "$CONFIGURATION" == "Profiling(Release)" ]; then
  CONFIGURATION=Release
fi

echo "Generating CommonAssemblyVersion.cs as $VERSION..."
$VFS_SCRIPTDIR/GenerateCommonAssemblyVersion.sh $VERSION || exit 1

# /warnasmessage:MSB4011. Reference: https://bugzilla.xamarin.com/show_bug.cgi?id=58564
# Visual Studio Mac does not support explicit import of Sdks. GSD.Installer.Mac.csproj
# does need this ability to override "Build" and "Publish" targets. As a workaround the 
# project implicitly imports "Microsoft.Net.Sdk" in the beginning of its csproj (because 
# otherwise Visual Studio Mac IDE will not be able to open the GSD.Install.Mac project) 
# and explicitly imports Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" later, before 
# overriding build targets. The duplicate import generates warning MSB4011 that is ignored
# by this switch.
echo 'Restoring packages...'
dotnet restore $VFS_SRCDIR/GSD.sln /p:Configuration=$CONFIGURATION.Mac --packages $VFS_PACKAGESDIR /warnasmessage:MSB4011 || exit 1
dotnet build $VFS_SRCDIR/GSD.sln --runtime osx-x64 --framework netcoreapp2.1 --configuration $CONFIGURATION.Mac -p:CopyPrjFS=true /maxcpucount:1 /warnasmessage:MSB4011 || exit 1

NATIVEDIR=$VFS_SRCDIR/GSD/GSD.Native.Mac
xcodebuild -configuration $CONFIGURATION -workspace $NATIVEDIR/GSD.Native.Mac.xcworkspace build -scheme GSD.Native.Mac -derivedDataPath $VFS_OUTPUTDIR/GSD.Native.Mac || exit 1

USERNOTIFICATIONDIR=$VFS_SRCDIR/GSD/GSD.Notifications/GSD.Mac
USERNOTIFICATIONPROJECT="$USERNOTIFICATIONDIR/GSD.xcodeproj"
USERNOTIFICATIONSCHEME="GSD"
updateAppVersionCmd="(cd \"$USERNOTIFICATIONDIR\" && /usr/bin/xcrun agvtool new-marketing-version \"$VERSION\")"
echo $updateAppVersionCmd
eval $updateAppVersionCmd
# Build user notification app
xcodebuild -configuration $CONFIGURATION -project "$USERNOTIFICATIONPROJECT" build -scheme "$USERNOTIFICATIONSCHEME" -derivedDataPath $VFS_OUTPUTDIR/GSD.Notifications/GSD.Mac || exit 1

# Build the tests in a separate directory, so the binary for distribution does not contain
# test plugins created and injected by the test build.
xcodebuild -configuration $CONFIGURATION -project "$USERNOTIFICATIONPROJECT" test -scheme "$USERNOTIFICATIONSCHEME" -derivedDataPath $VFS_OUTPUTDIR/GSD.Notifications/GSD.Mac/Tests || exit 1

if [ ! -d $VFS_PUBLISHDIR ]; then
  mkdir $VFS_PUBLISHDIR || exit 1
fi

echo 'Copying native binaries to Publish directory...'
cp $VFS_OUTPUTDIR/GSD.Native.Mac/Build/Products/$CONFIGURATION/GSD.ReadObjectHook $VFS_PUBLISHDIR || exit 1
dotnet publish $VFS_SRCDIR/GSD.sln /p:Configuration=$CONFIGURATION.Mac /p:Platform=x64 --runtime osx-x64 --framework netcoreapp2.1 --self-contained --output $VFS_PUBLISHDIR /maxcpucount:1 /warnasmessage:MSB4011 || exit 1

echo 'Copying Git installer to the output directory...'
$VFS_SCRIPTDIR/PublishGit.sh $GITPATH || exit 1

echo 'Installing shared data queue stall workaround...'
# We'll generate a temporary project if and only if we don't find the correct dylib already in place.
BUILDDIR=$VFS_OUTPUTDIR/GSD.Build
if [ ! -e $BUILDDIR/libSharedDataQueue.dylib ]; then
  cp $VFS_SRCDIR/nuget.config $BUILDDIR
  dotnet new classlib -n Restore.SharedDataQueueStallWorkaround -o $BUILDDIR --force
  dotnet add $BUILDDIR/Restore.SharedDataQueueStallWorkaround.csproj package --package-directory $VFS_PACKAGESDIR SharedDataQueueStallWorkaround --version '1.0.0'
  cp $VFS_PACKAGESDIR/shareddataqueuestallworkaround/1.0.0/libSharedDataQueue.dylib $BUILDDIR/libSharedDataQueue.dylib
fi

echo 'Running GSD unit tests...'
$VFS_PUBLISHDIR/GSD.UnitTests || exit 1
