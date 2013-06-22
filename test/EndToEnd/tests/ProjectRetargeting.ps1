function Test-ProjectRetargeting-ShowErrorMessage {
    param($context)

    # Arrange
    $p = New-ClassLibrary

    $p | Install-Package PackageWithNet40AndSLLibButOnlySLGacRefs -Source $context.RepositoryRoot

    Assert-Package $p 'PackageWithNet40AndSLLibButOnlySLGacRefs'

	$errorlistBeforeAct = Get-Errors

    # Act (change the target framework of the project to 3.5 and verify that an error is thrown )

    $projectName = $p.Name
    $p.Properties.Item("TargetFrameworkMoniker").Value = '.NETFramework,Version=3.5'

	# Assert (Assert that an error has been added to the error list window)

	$errorlist = Get-Errors

	Assert-AreEqual 1 ($errorlist.Count - $errorlistBeforeAct.Count)

	$error = $errorlist.Item($errorlist.Count)

	Assert-AreEqual 'Some NuGet packages were installed using a target framework different from the current target framework and may need to be reinstalled. For more information, visit http://docs.nuget.org/workflows/reinstalling-packages.  Packages affected: PackageWithNet40AndSLLibButOnlySLGacRefs' $error.Description
}

function Test-ProjectRetargeting-ErrorMessageIsCleanedUponBuild {
    param($context)

    # Arrange
    $p = New-ClassLibrary

    $p | Install-Package PackageWithNet40AndSLLibButOnlySLGacRefs -Source $context.RepositoryRoot

    Assert-Package $p 'PackageWithNet40AndSLLibButOnlySLGacRefs'

	$errorlistBeforeAct = Get-Errors

    # Act (change the target framework of the project to 3.5 and verify that an error is thrown )

    $projectName = $p.Name
    $p.Properties.Item("TargetFrameworkMoniker").Value = '.NETFramework,Version=3.5'

	# Assert (Assert that an error has been added to the error list window)

	$errorlist = Get-Errors

	Assert-AreEqual 1 ($errorlist.Count - $errorlistBeforeAct.Count)

	Build-Solution

	$errorlist = Get-Errors

	Assert-AreEqual 0 $errorlist.Count
}

function Test-ProjectRetargeting-ErrorMessageIsCleanedUponCleanProject {
    param($context)

    # Arrange
    $p = New-ClassLibrary

    $p | Install-Package PackageWithNet40AndSLLibButOnlySLGacRefs -Source $context.RepositoryRoot

    Assert-Package $p 'PackageWithNet40AndSLLibButOnlySLGacRefs'

	$errorlistBeforeAct = Get-Errors

    # Act (change the target framework of the project to 3.5 and verify that an error is thrown )

    $projectName = $p.Name
    $p.Properties.Item("TargetFrameworkMoniker").Value = '.NETFramework,Version=3.5'

	# Assert (Assert that an error has been added to the error list window)

	$errorlist = Get-Errors

	Assert-AreEqual 1 ($errorlist.Count - $errorlistBeforeAct.Count)

	Clean-Project

	$errorlist = Get-Errors

	Assert-AreEqual 0 $errorlist.Count
}

function Test-ProjectRetargeting-ErrorMessageIsCleanedUponCloseSolution {
    param($context)

    # Arrange
    $p = New-ClassLibrary

    $p | Install-Package PackageWithNet40AndSLLibButOnlySLGacRefs -Source $context.RepositoryRoot

    Assert-Package $p 'PackageWithNet40AndSLLibButOnlySLGacRefs'

	$errorlistBeforeAct = Get-Errors

    # Act (change the target framework of the project to 3.5 and verify that an error is thrown )

    $projectName = $p.Name
    $p.Properties.Item("TargetFrameworkMoniker").Value = '.NETFramework,Version=3.5'

	# Assert (Assert that an error has been added to the error list window)

	$errorlist = Get-Errors

	Assert-AreEqual 1 ($errorlist.Count - $errorlistBeforeAct.Count)

	Close-Solution

	$errorlist = Get-Errors

	Assert-AreEqual 0 $errorlist.Count
}

function Test-ProjectRetargeting-ErrorMessageIsCleanedAfterRetargetingBackToOriginalFramework {
    param($context)

    # Arrange
    $p = New-ClassLibrary

    $p | Install-Package PackageWithNet40AndSLLibButOnlySLGacRefs -Source $context.RepositoryRoot

    Assert-Package $p 'PackageWithNet40AndSLLibButOnlySLGacRefs'

	$errorlistBeforeAct = Get-Errors

    # Act (change the target framework of the project to 3.5 and verify that an error is thrown )

    $projectName = $p.Name
    $p.Properties.Item("TargetFrameworkMoniker").Value = '.NETFramework,Version=3.5'

	# Assert (Assert that an error has been added to the error list window)

	$errorlist = Get-Errors

	Assert-AreEqual 1 ($errorlist.Count - $errorlistBeforeAct.Count)

	# Change the framework of the project back to .NET 4.0 and verify that the error shown is cleared

	$p = Get-Project $projectName
	$p.Properties.Item("TargetFrameworkMoniker").Value = '.NETFramework,Version=4.0'

	$errorlist = Get-Errors

	Assert-AreEqual 0 $errorlist.Count
}