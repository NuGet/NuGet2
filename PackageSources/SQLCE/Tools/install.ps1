param([string]$ToolsDir)

. (Join-Path $ToolsDir "GetSqlCEPostBuildCmd.ps1")

# Get the current Post Build Event cmd
$currentPostBuildCmd = $proj.Properties.Item("PostBuildEvent").Value

# Append our post build command if it's not already there
if (!$currentPostBuildCmd.Contains($SqlCEPostBuildCmd)) {
    $proj.Properties.Item("PostBuildEvent").Value += $SqlCEPostBuildCmd
}
