
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
    Install-Package elmah -Project $p.Name
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

function Test-GetPackageThrowsWithInvalidSourceName {
    # Act
    Assert-Throws { 
        Get-Package -ListAvailable -Source 'nonexistent package source'
    } 'Invalid URI: The format of the URI could not be determined.'
}

function Test-GetPackageAcceptsAllAsSourceName {
     # Act
    $p = @(Get-Package -Filter elmah -ListAvailable -Source 'All')

    # Assert
    Assert-True (1 -le $p.Count)
}