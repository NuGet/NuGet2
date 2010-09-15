# TODO: get the right project!
$proj = $dte.Solution.Projects.Item(1)

$NativeAssembliesDir = Join-Path $ToolsDir "..\NativeBinaries"

$SqlCEPostBuildCmd = "
if not exist `$(TargetDir)x86 md `$(TargetDir)x86
copy $(Join-Path $NativeAssembliesDir "x86\*.*") `$(TargetDir)x86
if not exist `$(TargetDir)amd64 md `$(TargetDir)amd64
copy $(Join-Path $NativeAssembliesDir "amd64\*.*") `$(TargetDir)amd64"
