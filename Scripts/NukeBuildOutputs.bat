@ECHO OFF
CALL %~dp0\InitializeEnvironment.bat || EXIT /b 10

taskkill /f /im GSD.Mount.exe 2>&1
verify >nul

powershell -NonInteractive -NoProfile -Command "& { (Get-MpPreference).ExclusionPath | ? {$_.StartsWith('C:\Repos\')} | %%{Remove-MpPreference -ExclusionPath $_} }"

IF EXIST C:\Repos\GSDFunctionalTests\enlistment (
    rmdir /s /q C:\Repos\GSDFunctionalTests\enlistment
) ELSE (
    ECHO no test enlistment found
)

IF EXIST C:\Repos\GSDPerfTest (
    rmdir /s /q C:\Repos\GSDPerfTest
) ELSE (
    ECHO no perf test enlistment found
)

IF EXIST %VFS_OUTPUTDIR% (
    ECHO deleting build outputs
    rmdir /s /q %VFS_OUTPUTDIR%
) ELSE (
    ECHO no build outputs found
)

IF EXIST %VFS_PUBLISHDIR% (
    ECHO deleting published output
    rmdir /s /q %VFS_PUBLISHDIR%
) ELSE (
    ECHO no packages found
)

IF EXIST %VFS_PACKAGESDIR% (
    ECHO deleting packages
    rmdir /s /q %VFS_PACKAGESDIR%
) ELSE (
    ECHO no packages found
)

call %VFS_SCRIPTSDIR%\StopAllServices.bat
