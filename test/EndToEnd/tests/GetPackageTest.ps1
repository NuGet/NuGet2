
function Test-GetPackageRetunsMoreThanServerPagingLimit {
    # Act
    $packages = Get-Package -ListAvailable -Source 'https://go.microsoft.com/fwlink/?LinkID=206669'
    
    # Assert
    Assert-True $packages.Count -gt 100 "Get-Package cmdlet returns less than (or equal to) than server side paging limit"
}

function Test-GetPackageListsInstalledPackages {
    # Arrange
    $p = New-WebApplication
    
    # Act
    Install-Package elmah -Project $p.Name -Version 1.1
    Install-Package jQuery -Project $p.Name    
    $packages = Get-Package
    
    # Assert
    Assert-AreEqual 2 $packages.Count
}

function Test-GetPackageWithoutOpenSolutionThrows {
    Assert-Throws { Get-Package } "The current environment doesn't have a solution open."
}

function Test-GetPackageWithUpdatesListsUpdates {
    # Arrange
    $p = New-WebApplication
    
    # Act
    Install-Package Antlr -Version 3.1.1 -Project $p.Name
    Install-Package jQuery -Version 1.4.1 -Project $p.Name    
    $packages = Get-Package -Updates
    
    # Assert
    Assert-AreEqual 2 $packages.Count
}

function Test-GetPackageListsRecentPackages {
    # Arrange
    $p = New-ConsoleApplication

    # Act
    Install-Package Antlr -Project $p.Name
    Install-Package jQuery -Project $p.Name

    $packages = Get-Package -Recent
    $haveAntlr = @($packages | Where-Object { $_.Id -eq 'Antlr' })
    $havejQuery = @($packages | Where-Object { $_.Id -eq 'jQuery' })

    # Assert
    Assert-True ($haveAntlr.Count -gt 0)
    Assert-True ($havejQuery.Count -gt 0)
}


function Test-GetPackageReturnPackagesInOrderOfLastInstalledFirst {
    # Arrange
    $p = New-ConsoleApplication

    # Act
    Install-Package Antlr -Project $p.Name
    Install-Package jQuery -Project $p.Name
    Install-Package elmah -Project $p.Name

    $packages = Get-Package -Recent -First 3

    # Assert
    Assert-AreEqual 3 $packages.Count
    Assert-AreEqual "elmah" $packages[0].Id
    Assert-AreEqual "jQuery" $packages[1].Id
    Assert-AreEqual "Antlr" $packages[2].Id
}

function Test-GetPackageCollapsesPackageVersionsForListAvailable {
    # Act
    $packages = Get-Package -ListAvailable jQuery 
    $packagesWithMoreThanOne = $packages | group "Id" | Where { $_.count -gt 1 } 
    
    # Assert
    # Ensure we have at least some packages
    Assert-True (1 -le $packages.Count)
    Assert-Null $packagesWithMoreThanOne
}    

function Test-GetPackageAllVersionReturnsMultipleVersions {
    # Act
    $packages = Get-Package -AllVersions -Remote jQuery
    $packageWithMoreThanOneVersion = $packages | group "Id" | Where { $_.count -gt 1 } 

    # Assert
    # Ensure we have at least some packages
    Assert-True (1 -le $packages.Count) 
    Assert-True ($packageWithMoreThanOneVersion.Count -gt 0)
}


function Test-GetPackageCollapsesPackageVersionsForRecent {
    # Arrange
    $p = New-ConsoleApplication
    
    # Act
    Install-Package jQuery -Project $p.Name -Version 1.4.1
    Install-Package jQuery -Project $p.Name -Version 1.4.2
    Install-Package jQuery -Project $p.Name -Version 1.4.4
    Install-Package Antlr -Project $p.Name
        
    $recentPackages = Get-Package -Recent
    $packagesWithMoreThanOneVersions = $recentPackages | group "Id" | Where { $_.count -gt 1 } 
 
    # Assert
    # Ensure we have at least some packages
    Assert-True (1 -le $recentPackages.Count) 
    Assert-Null $packagesWithMoreThanOneVersions
}

function Test-GetPackageAcceptsSourceName {
    # Act
    $p = @(Get-Package -Filter elmah -ListAvailable -Source 'NuGet official package source')

    # Assert
    Assert-True (1 -le $p.Count)
}

function Test-GetPackageWithUpdatesAcceptsSourceName {
    # Arrange
    $p = New-WebApplication
    
    # Act
    Install-Package Antlr -Version 3.1.1 -Project $p.Name -Source 'NUGET OFFICIAL PACKAGE SOURCE'
    Install-Package jQuery -Version 1.4.1 -Project $p.Name -Source 'NUGET OFFICIAL PACKAGE SOURCE'
    $packages = Get-Package -Updates -Source 'NUGET OFFICIAL PACKAGE SOURCE'
    
    # Assert
    Assert-AreEqual 2 $packages.Count
}

function Test-GetPackageAcceptsAllAsSourceName {
     # Act
    $p = @(Get-Package -Filter elmah -ListAvailable -Source 'All')

    # Assert
    Assert-True (1 -le $p.Count)
}

function Test-GetPackageAcceptsRelativePathSource {
    param(
        $context
    )

    pushd

    # Act
    cd $context.TestRoot
    $p = @(Get-Package -ListAvailable -Source '..\')

    # Assert
    Assert-True (1 -le $p.Count)

    popd
}

function Test-GetPackageAcceptsRelativePathSource2 {
    param(
        $context
    )

    pushd

    # Arrange
    $repositoryRoot = $context.RepositoryRoot
    $parentOfRoot = Split-Path $repositoryRoot
    $relativePath = Split-Path $repositoryRoot -Leaf

    # Act
    cd $parentOfRoot
    $p = @(Get-Package -ListAvailable -Source $relativePath)

    # Assert
    Assert-True (1 -le $p.Count)

    popd
}

function Test-GetPackageThrowsWhenSourceIsInvalid {
    # Act & Assert
    Assert-Throws { Get-Package -ListAvailable -source "d:package" } "Invalid URI: A Dos path must be rooted, for example, 'c:\'."
}

function Test-UpdatedPackageAppearInRecentPackageList {
    # Arrange
    Clear-RecentPackageRepository

    $p = New-ConsoleApplication
    Install-Package jQuery -Version 1.5
    Update-Package jQuery -Version 1.6

    # Act
    $result = @(Get-Package -Recent)

    # Assert
    Assert-AreEqual 1 $result.Count
    Assert-AreEqual "jQuery" $result[0].Id
    Assert-AreEqual "1.6" $result[0].Version
}

function Test-GetPackageForProjectReturnsEmptyProjectIfItHasNoInstalledPackage {
    # Arrange
    $p = New-ConsoleApplication

    # Act
    $result = @(Get-Package -ProjectName $p.Name)

    # Assert
    Assert-AreEqual 0 $result.Count
}

function Test-GetPackageForProjectReturnsCorrectPackages {
    # Arrange
    $p = New-ConsoleApplication
    Install-Package jQuery -Version 1.5 -Source $context.RepositoryRoot

    # Act
    $result = @(Get-Package -ProjectName $p.Name)

    # Assert
    Assert-AreEqual 1 $result.Count
    Assert-AreEqual "jQuery" $result[0].Id
    Assert-AreEqual "1.5" $result[0].Version
}

function Test-GetPackageForProjectReturnsCorrectPackages2 {
    # Arrange
    $p1 = New-ConsoleApplication
    $p2 = New-ClassLibrary

    Install-Package jQuery -Version 1.5 -Source $context.RepositoryRoot -ProjectName $p1.Name
    Install-Package MyAwesomeLibrary -Version 1.0 -Source $context.RepositoryRoot -ProjectName $p2.Name

    # Act
    $result = @(Get-Package -ProjectName $p1.Name)

    # Assert
    Assert-AreEqual 1 $result.Count
    Assert-AreEqual "jQuery" $result[0].Id
    Assert-AreEqual "1.5" $result[0].Version
}

function Test-GetPackageForProjectReturnsEmptyIfItHasNoInstalledPackage {
    # Arrange
    $p = New-ConsoleApplication

    # Act
    $result = @(Get-Package -ProjectName $p.Name)

    # Assert
    Assert-AreEqual 0 $result.Count
}

function Test-GetPackageForProjectReturnsEmptyIfItHasNoInstalledPackage2 {
    param(
        $context
    )

    # Arrange
    $p1 = New-ConsoleApplication
    $p2 = New-ClassLibrary

    Install-Package jQuery -Source $context.RepositoryRoot -Project $p1.Name
 
    # Act
    $result = @(Get-Package -ProjectName $p2.Name)

    # Assert
    Assert-AreEqual 0 $result.Count
}

function Test-GetPackageForProjectThrowIfProjectNameIsInvalid {
    param(
        $context
    )

    # Arrange
    $p1 = New-ConsoleApplication
 
    # Act & Assert
    Assert-Throws { Get-Package -ProjectName "invalidname" } "No compatible project(s) found in the active solution."
}