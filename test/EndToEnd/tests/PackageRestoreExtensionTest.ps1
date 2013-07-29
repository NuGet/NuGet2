# Tests that packages are restored on build
function Test-PackageRestore-SimpleTest {
    param($context)

	# Arrange
	$p1 = New-ClassLibrary	
	$p1 | Install-Package FakeItEasy -version 1.8.0
	
	$p2 = New-ClassLibrary
	$p2 | Install-Package elmah -Version 1.1

	# delete the packages folder
	$packagesDir = Get-PackagesDir
	Remove-Item -Recurse -Force $packagesDir
	Assert-False (Test-Path $packagesDir)

	# Act
	Build-Solution

	# Assert
	Assert-True (Test-Path $packagesDir)
	Assert-Package $p1 FakeItEasy
	Assert-Package $p2 elmah
}

# Tests that package restore works for website project
function Test-PackageRestore-Website {
    param($context)

	# Arrange
	$p = New-WebSite	
	$p | Install-Package JQuery
	
	# delete the packages folder
	$packagesDir = Get-PackagesDir
	Remove-Item -Recurse -Force $packagesDir
	Assert-False (Test-Path $packagesDir)

	# Act
	Build-Solution

	# Assert
	Assert-True (Test-Path $packagesDir)
	Assert-Package $p JQuery
}

# Tests that package restore works for JavaScript Metro project
function Test-PackageRestore-JavaScriptMetroProject {
    param($context)

    if ($dte.Version -eq '10.0') {
        return
    }

	# Arrange
	$p = New-JavaScriptApplication	
	Install-Package JQuery -projectName $p.Name
	
	# delete the packages folder
	$packagesDir = Get-PackagesDir
	Remove-Item -Recurse -Force $packagesDir
	Assert-False (Test-Path $packagesDir)

	# Act
	Build-Solution

	# Assert
	Assert-True (Test-Path $packagesDir)
	Assert-Package $p JQuery
}

# Tests that package restore works for unloaded projects, as long as
# there is at least one loaded project.
function Test-PackageRestore-UnloadedProjects{
    param($context)

	# Arrange
	$p1 = New-ClassLibrary	
	$p1 | Install-Package Microsoft.Bcl.Build -version 1.0.8
	
	$p2 = New-ClassLibrary

	$solutionDir = $dte.Solution.FullName
	$packagesDir = Get-PackagesDir
	$dte.Solution.SaveAs($solutionDir)
    Close-Solution

	# delete the packages folder
	Remove-Item -Recurse -Force $packagesDir
	Assert-False (Test-Path $packagesDir)

	# reopen the solution. Now the project that references Microsoft.Bcl.Build
	# will not be loaded because of missing targets file
	Open-Solution $solutionDir

	# Act
	Build-Solution

	# Assert
	$dir = Join-Path $packagesDir "Microsoft.Bcl.Build.1.0.8"
	Assert-PathExists $dir
}

# Tests that an error will be generated if package restore fails
function Test-PackageRestore-ErrorMessage {
    param($context)

	# Arrange
	$p = New-ClassLibrary	
	Install-Package -Source $context.RepositoryRoot -Project $p.Name NonStrongNameB
	
	# delete the packages folder
	$packagesDir = Get-PackagesDir
	Remove-Item -Recurse -Force $packagesDir
	Assert-False (Test-Path $packagesDir)

	# Act
    # package restore will fail because the source $context.RepositoryRoot is not
    # listed in the settings.
	Build-Solution

	# Assert
    $errorlist = Get-Errors
    Assert-AreEqual 1 $errorlist.Count

    $error = $errorlist[$errorlist.Count-1]
    Assert-True ($error.Description.Contains('NuGet Package restore failed for project'))

    $output = GetBuildOutput
    Assert-True ($output.Contains('NuGet package restore failed.'))
}

function GetBuildOutput { 
    $dte2 = Get-Interface $dte ([EnvDTE80.DTE2])
    $buildPane = $dte2.ToolWindows.OutputWindow.OutputWindowPanes.Item("Build")
    $doc = $buildPane.TextDocument
    $sel = $doc.Selection
    $sel.StartOfDocument($FALSE)
    $sel.EndOfDocument($TRUE)
    $sel.Text
}