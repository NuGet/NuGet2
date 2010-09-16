param([string]$ToolsDir)

. (Join-Path $ToolsDir "GetSqlCEPostBuildCmd.ps1")

# Get the current Post Build Event cmd
$currentPostBuildCmd = $proj.Properties.Item("PostBuildEvent").Value

# Remove our post build command from it (if it's there)
$proj.Properties.Item("PostBuildEvent").Value = $currentPostBuildCmd.Replace($SqlCEPostBuildCmd, "")
