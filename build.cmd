@echo Off
set config=%1
if "%config%" == "" (
   set config=debug
)
pushd %~dp0

if exist bin goto build
mkdir bin

:Build

%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild Build\Build.proj /p:Configuration="%config%" /m /v:M /fl /flp:LogFile=bin\msbuild.log;Verbosity=Normal