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

	# Arrange
	$p = New-JavaScriptApplication	
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