@echo Off
SETLOCAL
set config=%1

if "%config%" == "" (
   set config=debug
)

REM Some unit-tests may leave nuget.config files in %TEMP% which leads to hard-to-debug failures
FOR /F "tokens=*" %%I IN ('dir /s /b "%TEMP%\nuget.config" 2^>NUL') DO DEL "%%I"

REM Dev10 and Dev11 msbuild path
set nugetmsbuildpath="%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild"

REM Dev12 msbuild path
set nugetmsbuildpathtmp="%ProgramFiles%\MSBuild\12.0\bin\msbuild"
if exist %nugetmsbuildpathtmp% set nugetmsbuildpath=%nugetmsbuildpathtmp%
set nugetmsbuildpathtmp="%ProgramFiles(x86)%\MSBuild\12.0\bin\msbuild"
if exist %nugetmsbuildpathtmp% set nugetmsbuildpath=%nugetmsbuildpathtmp%

REM Dev14 msbuild path
set nugetmsbuildpathtmp="%ProgramFiles%\MSBuild\14.0\bin\msbuild"
if exist %nugetmsbuildpathtmp% set nugetmsbuildpath=%nugetmsbuildpathtmp%
set nugetmsbuildpathtmp="%ProgramFiles(x86)%\MSBuild\14.0\bin\msbuild"
if exist %nugetmsbuildpathtmp% set nugetmsbuildpath=%nugetmsbuildpathtmp%

set EnableNuGetPackageRestore=true 
%nugetmsbuildpath% Build\Build.proj /p:Configuration="%config%" /p:Platform="Any CPU" /m /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Detailed /nr:false 
ENDLOCAL