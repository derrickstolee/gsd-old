@ECHO OFF
CALL %~dp0\InitializeEnvironment.bat || EXIT /b 10

IF "%1"=="" (SET "Configuration=Debug") ELSE (SET "Configuration=%1")

SETLOCAL
SET PATH=C:\Program Files\GSD;C:\Program Files\Git\cmd;%PATH%

if not "%2"=="--test-gsd-on-path" goto :startFunctionalTests

REM Force GSD.FunctionalTests.exe to use the installed version of GSD
del %VFS_OUTPUTDIR%\GSD.FunctionalTests\bin\x64\%Configuration%\netcoreapp2.1\GitHooksLoader.exe
del %VFS_OUTPUTDIR%\GSD.FunctionalTests\bin\x64\%Configuration%\netcoreapp2.1\GSD.exe
del %VFS_OUTPUTDIR%\GSD.FunctionalTests\bin\x64\%Configuration%\netcoreapp2.1\GSD.Hooks.exe
del %VFS_OUTPUTDIR%\GSD.FunctionalTests\bin\x64\%Configuration%\netcoreapp2.1\GSD.ReadObjectHook.exe
del %VFS_OUTPUTDIR%\GSD.FunctionalTests\bin\x64\%Configuration%\netcoreapp2.1\GSD.Mount.exe
del %VFS_OUTPUTDIR%\GSD.FunctionalTests\bin\x64\%Configuration%\netcoreapp2.1\GSD.Service.exe
del %VFS_OUTPUTDIR%\GSD.FunctionalTests\bin\x64\%Configuration%\netcoreapp2.1\GSD.Service.UI.exe

REM Same for GSD.FunctionalTests.Windows.exe
del %VFS_OUTPUTDIR%\GSD.FunctionalTests.Windows\bin\x64\%Configuration%\GitHooksLoader.exe
del %VFS_OUTPUTDIR%\GSD.FunctionalTests.Windows\bin\x64\%Configuration%\GSD.exe
del %VFS_OUTPUTDIR%\GSD.FunctionalTests.Windows\bin\x64\%Configuration%\GSD.Hooks.exe
del %VFS_OUTPUTDIR%\GSD.FunctionalTests.Windows\bin\x64\%Configuration%\GSD.ReadObjectHook.exe
del %VFS_OUTPUTDIR%\GSD.FunctionalTests.Windows\bin\x64\%Configuration%\GSD.Mount.exe
del %VFS_OUTPUTDIR%\GSD.FunctionalTests.Windows\bin\x64\%Configuration%\GSD.Service.exe
del %VFS_OUTPUTDIR%\GSD.FunctionalTests.Windows\bin\x64\%Configuration%\GSD.Service.UI.exe

echo PATH = %PATH%
echo gvfs location:
where gsd
echo GSD.Service location:
where GSD.Service
echo git location:
where git

:startFunctionalTests
dotnet %VFS_OUTPUTDIR%\GSD.FunctionalTests\bin\x64\%Configuration%\netcoreapp2.1\GSD.FunctionalTests.dll /result:TestResultNetCore.xml %2 %3 %4 %5 || goto :endFunctionalTests
%VFS_OUTPUTDIR%\GSD.FunctionalTests.Windows\bin\x64\%Configuration%\GSD.FunctionalTests.Windows.exe /result:TestResultNetFramework.xml --windows-only %2 %3 %4 %5 || goto :endFunctionalTests

:endFunctionalTests
set error=%errorlevel%

call %VFS_SCRIPTSDIR%\StopAllServices.bat

exit /b %error%
