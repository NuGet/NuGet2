@echo Off
set config=%1
if "%config%" == "" (
   set config=release
)
set deployUrl=%2
if "%deployUrl%" == "" (
   set deployUrl=http://nuget.codeplex.com/releases/clickonce/
)
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild build\build.proj /p:Configuration="%config%";DeploymentUrl="%deployUrl%"