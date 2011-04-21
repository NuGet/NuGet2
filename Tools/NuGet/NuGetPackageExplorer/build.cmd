@echo Off

set deployUrl=%1
if "%deployUrl%" == "" (
   set deployUrl=http://nuget.codeplex.com/releases/clickonce/
)
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild build\build.proj /p:Configuration=release;DeploymentUrl="%deployUrl%";EnableCodeAnalysis=true