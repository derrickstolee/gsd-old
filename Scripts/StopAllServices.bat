@ECHO OFF
CALL %~dp0\InitializeEnvironment.bat || EXIT /b 10

call %VFS_SCRIPTSDIR%\StopService.bat GSD.Service
call %VFS_SCRIPTSDIR%\StopService.bat Test.GSD.Service
