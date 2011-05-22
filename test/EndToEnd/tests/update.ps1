function Test-UpdatingPackageInProjectDoesntRemoveFromSolutionIfInUse {
    # Arrange
    $p1 = New-WebApplication
    $p2 = New-ClassLibrary 

    $oldReferences = @("Castle.Core", 
                       "Castle.Services.Logging.log4netIntegration", 
                       "Castle.Services.Logging.NLogIntegration", 
                       "log4net",
                       "NLog")
                       
    Install-Package Castle.Core -Version 1.2.0 -Project $p1.Name
    $oldReferences | %{ Assert-Reference $p1 $_ }
    
    Install-Package Castle.Core -Version 1.2.0 -Project $p2.Name
    $oldReferences | %{ Assert-Reference $p2 $_ }
    
    # Check that it's installed at solution level
    Assert-SolutionPackage Castle.Core 1.2.0
    
    # Update the package in the first project
    Update-Package Castle.Core -Project $p1.Name -Version 2.5.1
    Assert-Reference $p1 Castle.Core 2.5.1.0
    Assert-SolutionPackage Castle.Core 2.5.1
    Assert-SolutionPackage Castle.Core 1.2.0
    
    # Update the package in the second project
    Update-Package Castle.Core -Project $p2.Name -Version 2.5.1
    Assert-Reference $p2 Castle.Core 2.5.1.0
    
    # Make sure that the old one is removed since no one is using it
    Assert-Null (Get-SolutionPackage Castle.Core 1.2.0)
}

function Test-UpdatingPackageWithSharedDependency {
    param(
        $context
    )
    
    # Arrange
    $p = New-ClassLibrary

    # Act
    Install-Package D -Version 1.0 -Source $context.RepositoryPath
    Assert-Package $p D 1.0
    Assert-Package $p B 1.0
    Assert-Package $p C 1.0
    Assert-Package $p A 2.0
    Assert-SolutionPackage D 1.0
    Assert-SolutionPackage B 1.0
    Assert-SolutionPackage C 1.0
    Assert-SolutionPackage A 2.0
    Assert-Null (Get-SolutionPackage A 1.0)
    
    Update-Package D -Source $context.RepositoryPath
    # Make sure the new package is installed
    Assert-Package $p D 2.0
    Assert-Package $p B 2.0
    Assert-Package $p C 2.0
    Assert-Package $p A 3.0
    Assert-SolutionPackage D 2.0
    Assert-SolutionPackage B 2.0
    Assert-SolutionPackage C 2.0
    Assert-SolutionPackage A 3.0
    
    # Make sure the old package is removed
    Assert-Null (Get-ProjectPackage $p D 1.0)
    Assert-Null (Get-ProjectPackage $p B 1.0)
    Assert-Null (Get-ProjectPackage $p C 1.0)
    Assert-Null (Get-ProjectPackage $p A 2.0)
    Assert-Null (Get-SolutionPackage D 1.0)
    Assert-Null (Get-SolutionPackage B 1.0)
    Assert-Null (Get-SolutionPackage C 1.0)
    Assert-Null (Get-SolutionPackage A 2.0)
    Assert-Null (Get-SolutionPackage A 1.0)
}

function Test-UpdateWithoutPackageInstalledThrows {
    # Arrange
    $p = New-ClassLibrary

    # Act & Assert
    Assert-Throws { $p | Update-Package elmah } "Unable to find package 'elmah'."
}

function Test-UpdateSolutionOnlyPackage {
    param(
        $context
    )

    # Arrange
    $p = New-WebApplication
    $solutionDir = Get-SolutionDir

    # Act
    $p | Install-Package SolutionOnlyPackage -Source $context.RepositoryRoot -Version 1.0
    Assert-SolutionPackage SolutionOnlyPackage 1.0
    Assert-PathExists (Join-Path $solutionDir packages\SolutionOnlyPackage.1.0\file1.txt)    

    $p | Update-Package SolutionOnlyPackage -Source $context.RepositoryRoot
    Assert-Null (Get-SolutionPackage SolutionOnlyPackage 1.0)
    Assert-SolutionPackage SolutionOnlyPackage 2.0
    Assert-PathExists (Join-Path $solutionDir packages\SolutionOnlyPackage.2.0\file2.txt)
}

function Test-UpdateSolutionOnlyPackageWhenAmbiguous {
    param(
        $context
    )

    # Arrange
    $p = New-MvcApplication
    Install-Package SolutionOnlyPackage -Version 1.0 -Source $context.RepositoryRoot
    Install-Package SolutionOnlyPackage -Version 2.0 -Source $context.RepositoryRoot

    Assert-SolutionPackage SolutionOnlyPackage 1.0
    Assert-SolutionPackage SolutionOnlyPackage 2.0

    Assert-Throws { Update-Package SolutionOnlyPackage } "Unable to update 'SolutionOnlyPackage'. Found multiple versions installed."
}

function Test-UpdatePackageResolvesDependenciesAcrossSources {
    param(
        $context
    )
    
    # Arrange
    $p = New-ConsoleApplication

    # Act
    # Ensure Antlr is not avilable in local repo.
    Assert-Null (Get-Package -ListAvailable -Source $context.RepositoryRoot Antlr)
    # Install a package with no external dependency
    Install-Package PackageWithExternalDependency -Source $context.RepositoryRoot -Version 0.5
    # Upgrade to a version that has an external dependency
    Update-Package PackageWithExternalDependency -Source $context.RepositoryRoot

    # Assert
    Assert-Package $p PackageWithExternalDependency
    Assert-Package $p Antlr
}

function Test-UpdateAmbiguousProjectLevelPackageNoInstalledInProjectThrows {
    # Arrange
    $p1 = New-ClassLibrary
    $p2 = New-FSharpLibrary
    $p1 | Install-Package Antlr -Version 3.1.1
    $p2 | Install-Package Antlr -Version 3.1.3.42154
    Remove-ProjectItem $p1 packages.config
    Remove-ProjectItem $p2 packages.config

    Assert-SolutionPackage Antlr 3.1.1
    Assert-SolutionPackage Antlr 3.1.3.42154
    @($p1, $p2) | %{ Assert-Null (Get-ProjectPackage $_ Antlr) }

    # Act
    Assert-Throws { $p1 | Update-Package Antlr } "Unable to find package 'Antlr' in '$($p1.Name)'."
}

function Test-SubTreeUpdateWithDependencyInUse {
    param(
        $context
    )
    
    # Arrange
    $p = New-ClassLibrary
    
    # Act
    $p | Install-Package A -Source $context.RepositoryPath
    Assert-Package $p A 1.0
    Assert-Package $p B 1.0
    Assert-Package $p E 1.0
    Assert-Package $p F 1.0
    Assert-Package $p C 1.0
    Assert-Package $p D 1.0

    $p | Update-Package F -Source $context.RepositoryPath
    Assert-Package $p A 1.0
    Assert-Package $p B 1.0
    Assert-Package $p E 1.0
    Assert-Package $p F 2.0
    Assert-Package $p C 1.0
    Assert-Package $p D 1.0
    Assert-Package $p G 1.0
}

function Test-ComplexUpdateSubTree {
    param(
        $context
    )
    
    # Arrange
    $p = New-ClassLibrary
    
    # Act
    $p | Install-Package A -Source $context.RepositoryPath
    Assert-Package $p A 1.0
    Assert-Package $p B 1.0
    Assert-Package $p E 1.0
    Assert-Package $p F 1.0
    Assert-Package $p C 1.0
    Assert-Package $p D 1.0


    $p | Update-Package E -Source $context.RepositoryPath
    Assert-Package $p A 1.0
    Assert-Package $p B 1.0
    Assert-Package $p E 2.0
    Assert-Package $p F 1.0
    Assert-Package $p C 1.0
    Assert-Package $p D 1.0
    Assert-Package $p G 1.0
}

function Test-SubTreeUpdateWithConflict {
    param(
        $context
    )
    
    # Arrange
    $p = New-ClassLibrary
    
    # Act
    $p | Install-Package A -Source $context.RepositoryPath
    $p | Install-Package G -Source $context.RepositoryPath
    Assert-Package $p A 1.0
    Assert-Package $p B 1.0
    Assert-Package $p C 1.0
    Assert-Package $p D 1.0
    Assert-Package $p G 1.0

    Assert-Throws { $p | Update-Package C -Source $context.RepositoryPath } "Updating 'C 1.0' failed. Unable to find a version of 'G' that is compatible with 'C 2.0'."
    Assert-Null (Get-ProjectPackage $p C 2.0)
    Assert-Null (Get-SolutionPackage C 2.0)
    Assert-Package $p A 1.0
    Assert-Package $p B 1.0
    Assert-Package $p C 1.0
    Assert-Package $p D 1.0
    Assert-Package $p G 1.0
}

function Test-AddingBindingRedirectAfterUpdate {
    param(
        $context
    )
    
    # Arrange
    $p = New-WebApplication
    
    # Act
    $p | Install-Package A -Source $context.RepositoryPath
    Assert-Package $p A 1.0
    Assert-Package $p B 1.0
    $p | Install-Package C -Source $context.RepositoryPath
    Assert-Package $p C 1.0
    Assert-Package $p B 2.0
    Assert-Null (Get-SolutionPackage B 1.0)

    Build-Project $p

    $redirect = $p | Add-BindingRedirect

    Assert-AreEqual B $redirect.Name
    Assert-AreEqual '0.0.0.0-2.0.0.0' $redirect.OldVersion
    Assert-AreEqual '2.0.0.0' $redirect.NewVersion
}


function Test-UpdatePackageWithOlderVersionOfSharedDependencyInUse {
    param(
        $context
    )
    
    # Arrange
    $p = New-ClassLibrary

    # Act
    $p | Install-Package K -Source $context.RepositoryPath
    Assert-Package $p K 1.0
    Assert-Package $p A 1.0
    Assert-SolutionPackage K 1.0
    Assert-SolutionPackage A 1.0

    $p | Install-Package D -Version 1.0 -Source $context.RepositoryPath
    Assert-Package $p D 1.0
    Assert-Package $p B 1.0
    Assert-Package $p C 1.0
    Assert-SolutionPackage D 1.0
    Assert-SolutionPackage B 1.0
    Assert-SolutionPackage C 1.0

    $p | Update-Package D -Source $context.RepositoryPath
    Assert-Package $p K 1.0
    Assert-Package $p D 2.0
    Assert-Package $p B 2.0
    Assert-Package $p C 2.0
    Assert-Package $p G 1.0
    Assert-Package $p A 2.0
    Assert-SolutionPackage K 1.0
    Assert-SolutionPackage D 2.0
    Assert-SolutionPackage B 2.0
    Assert-SolutionPackage C 2.0
    Assert-SolutionPackage G 1.0
    Assert-SolutionPackage A 2.0

    # Make sure the old package(s) are removed
    Assert-Null (Get-ProjectPackage $p D 1.0)
    Assert-Null (Get-ProjectPackage $p B 1.0)
    Assert-Null (Get-ProjectPackage $p C 1.0)
    Assert-Null (Get-ProjectPackage $p A 1.0)
    Assert-Null (Get-SolutionPackage D 1.0)
    Assert-Null (Get-SolutionPackage B 1.0)
    Assert-Null (Get-SolutionPackage C 1.0)
    Assert-Null (Get-SolutionPackage A 1.0)
}

function Test-UpdatePackageAcceptsSourceName {
    # Arrange
    $p = New-ConsoleApplication
    Install-Package Antlr -Version 3.1.1 -Project $p.Name -Source 'NUGET OFFICIAL PACKAGE SOURCE'

    Assert-Package $p Antlr 3.1.1

    # Act
    Update-Package Antlr -Version 3.1.3.42154 -Project $p.Name -Source 'NuGet official Package Source'

    # Assert
    Assert-Package $p Antlr 3.1.3.42154
}

function Test-UpdatePackageAcceptsAllAsSourceName {
    # Arrange
    $p = New-ConsoleApplication
    Install-Package Antlr -Version 3.1.1 -Project $p.Name -Source 'All'

    Assert-Package $p Antlr 3.1.1

    # Act
    Update-Package Antlr -Version 3.1.3.42154 -Project $p.Name -Source 'All'

    # Assert
    Assert-Package $p Antlr 3.1.3.42154
}

function Test-UpdatePackageAcceptsRelativePathSource {
    param(
        $context
    )
    
    pushd

    # Arrange
    $p = New-ConsoleApplication
    Install-Package SkypePackage -Version 1.0 -Project $p.Name -Source $context.RepositoryRoot
    Assert-Package $p SkypePackage 1.0

    $testPathName = Split-Path $context.TestRoot -Leaf

    cd $context.RepositoryRoot
    Assert-AreEqual $context.RepositoryRoot $pwd

    # Act
    Update-Package SkypePackage -Version 2.0 -Project $p.Name -Source $testPathName

    # Assert
    Assert-Package $p SkypePackage 2.0

    popd
}

function Test-UpdatePackageAcceptsRelativePathSource2 {
    param(
        $context
    )
    
    pushd

    # Arrange
    $p = New-ConsoleApplication
    Install-Package SkypePackage -Version 1.0 -Project $p.Name -Source $context.RepositoryRoot
    Assert-Package $p SkypePackage 1.0

    cd $context.TestRoot
    Assert-AreEqual $context.TestRoot $pwd

    # Act
    Update-Package SkypePackage -Version 3.0 -Project $p.Name -Source '..\'

    # Assert
    Assert-Package $p SkypePackage 3.0

    popd
}

function Test-UpdateProjectLevelPackageNotInstalledInAnyProject {
    # Arrange
    $p1 = New-WebApplication

    # Act
    $p1 | Install-Package Ninject -Version 2.0.1.0
    Remove-ProjectItem $p1 packages.config
    

    # Assert
    Assert-Throws { Update-Package Ninject } "'Ninject' was not installed in any project. Update failed."
}

function Test-UpdatePackageInAllProjects {
    # Arrange
    $p1 = New-FSharpLibrary
    $p2 = New-WebApplication
    $p3 = New-ClassLibrary
    $p4 = New-WebSite

    # Act
    $p1 | Install-Package Ninject -Version 2.0.1.0
    $p2 | Install-Package Ninject -Version 2.1.0.76
    $p3 | Install-Package Ninject -Version 2.2.0.0
    $p4 | Install-Package Ninject -Version 2.2.1.0

    Assert-SolutionPackage Ninject 2.0.1.0
    Assert-SolutionPackage Ninject 2.1.0.76
    Assert-SolutionPackage Ninject 2.2.0.0
    Assert-SolutionPackage Ninject 2.2.1.0
    Assert-Package $p1 Ninject 2.0.1.0
    Assert-Package $p2 Ninject 2.1.0.76
    Assert-Package $p3 Ninject 2.2.0.0
    Assert-Package $p4 Ninject 2.2.1.0

    Update-Package Ninject

    # Assert
    Assert-SolutionPackage Ninject 2.2.1.4
    Assert-Package $p1 Ninject 2.2.1.4
    Assert-Package $p2 Ninject 2.2.1.4
    Assert-Package $p3 Ninject 2.2.1.4
    Assert-Package $p4 Ninject 2.2.1.4
    Assert-Null (Get-SolutionPackage Ninject 2.0.1.0)
    Assert-Null (Get-SolutionPackage Ninject 2.1.0.76)
    Assert-Null (Get-SolutionPackage Ninject 2.2.0.0)
    Assert-Null (Get-SolutionPackage Ninject 2.2.1.0)
}

function Test-UpdateAllPackagesInSolution {
    param(
        $context
    )

    # Arrange
    $p1 = New-WebApplication
    $p2 = New-ClassLibrary
    
    # Act
    $p1 | Install-Package A -Version 1.0 -Source $context.RepositoryPath
    $p2 | Install-Package C -Version 1.0 -Source $context.RepositoryPath
    
    Assert-SolutionPackage A 1.0
    Assert-SolutionPackage B 1.0
    Assert-SolutionPackage C 1.0
    Assert-SolutionPackage D 2.0
    Assert-Package $p1 A 1.0
    Assert-Package $p1 B 1.0
    Assert-Package $p2 C 1.0
    Assert-Package $p2 D 2.0

    Update-Package -Source $context.RepositoryPath
    # Assert
    Assert-Null (Get-SolutionPackage A 1.0)
    Assert-Null (Get-SolutionPackage B 1.0)
    Assert-Null (Get-SolutionPackage D 2.0)
    Assert-Null (Get-ProjectPackage $p1 A 1.0)
    Assert-Null (Get-ProjectPackage $p1 B 1.0)
    Assert-Null (Get-ProjectPackage $p2 D 2.0)
    Assert-SolutionPackage A 2.0
    Assert-SolutionPackage B 2.0
    Assert-SolutionPackage C 1.0
    Assert-SolutionPackage D 4.0
    Assert-SolutionPackage E 3.0
    Assert-Package $p1 A 2.0
    Assert-Package $p1 B 2.0
    Assert-Package $p2 C 1.0
    Assert-Package $p2 D 4.0
    Assert-Package $p2 E 3.0
}

function Test-UpdateScenariosWithConstraints {
    param(
        $context
    )

    # Arrange
    $p1 = New-WebApplication
    $p2 = New-ClassLibrary
    $p3 = New-WebSite

    $p1 | Install-Package A -Version 1.0 -Source $context.RepositoryPath
    $p2 | Install-Package C -Version 1.0 -Source $context.RepositoryPath
    $p3 | Install-Package E -Version 1.0 -Source $context.RepositoryPath

    Add-PackageConstraint $p1 A "[1.0, 2.0)"
    Add-PackageConstraint $p2 D "[1.0]"
    Add-PackageConstraint $p3 E "[1.0]"
     
    # Act
    Update-Package A -Source $context.RepositoryPath
    $gt = [char]0x2265
    Assert-Throws { Update-Package C -Source $context.RepositoryPath } "Unable to resolve dependency 'D ($gt 2.0)'.'D' has an additional constraint (= 1.0) defined in packages.config."
    Assert-Throws { Update-Package F -Source $context.RepositoryPath } "Updating 'F 1.0' failed. Unable to find a version of 'E' that is compatible with 'F 2.0'."

    # Assert
    Assert-Package $p1 A 1.0
    Assert-Package $p1 B 1.0
    Assert-SolutionPackage A 1.0
    Assert-SolutionPackage B 1.0

    Assert-Package $p2 C 1.0
    Assert-Package $p2 D 1.0
    Assert-SolutionPackage C 1.0
    Assert-SolutionPackage D 1.0

    Assert-Package $p3 E 1.0
    Assert-Package $p3 F 1.0
    Assert-SolutionPackage E 1.0
    Assert-SolutionPackage F 1.0
}

function Test-UpdateAllPackagesInSolutionWithSafeFlag {
    param(
        $context
    )

    # Arrange
    $p1 = New-WebApplication
    $p1 | Install-Package A -Version 1.0 -Source $context.RepositoryPath -IgnoreDependencies
    $p1 | Install-Package B -Version 1.0 -Source $context.RepositoryPath -IgnoreDependencies
    $p1 | Install-Package C -Version 1.0 -Source $context.RepositoryPath -IgnoreDependencies

    # Act    
    Update-Package -Source $context.RepositoryPath -Safe

    # Assert
    Assert-Package $p1 A 1.0.3
    Assert-Package $p1 B 1.0.3
    Assert-Package $p1 C 1.0.0.1
    Assert-SolutionPackage A 1.0.3
    Assert-SolutionPackage B 1.0.3
    Assert-SolutionPackage C 1.0.0.1
}

function Test-UpdatePackageWithSafeFlag {
    param(
        $context
    )

    # Arrange
    $p1 = New-WebApplication
    $p1 | Install-Package A -Version 1.0 -Source $context.RepositoryPath -IgnoreDependencies
    $p1 | Install-Package B -Version 1.0 -Source $context.RepositoryPath -IgnoreDependencies
    $p1 | Install-Package C -Version 1.0 -Source $context.RepositoryPath -IgnoreDependencies

    # Act    
    Update-Package A -Source $context.RepositoryPath -Safe

    # Assert
    Assert-Package $p1 A 1.0.3
    Assert-Package $p1 B 1.0.3
    Assert-Package $p1 C 1.0.0.1
    Assert-SolutionPackage A 1.0.3
    Assert-SolutionPackage B 1.0.3
    Assert-SolutionPackage C 1.0.0.1
}

function Test-UpdatePackageDiamondDependenciesBottomNodeConflictingPackages {
    param(
        $context
    )

    # Arrange
    $p = New-WebApplication
    $p | Install-Package A -Version 1.0 -Source $context.RepositoryPath

    # Act
    Update-Package D -Source $context.RepositoryPath

    # Assert
    Assert-Package $p A 2.0
    Assert-Package $p B 2.0
    Assert-Package $p C 2.0
    Assert-Package $p D 2.0
    Assert-SolutionPackage A 2.0
    Assert-SolutionPackage B 2.0
    Assert-SolutionPackage C 2.0
    Assert-SolutionPackage D 2.0

    Assert-Null (Get-ProjectPackage $p A 1.0)
    Assert-Null (Get-ProjectPackage $p B 1.0)
    Assert-Null (Get-ProjectPackage $p C 1.0)
    Assert-Null (Get-ProjectPackage $p D 1.0)
    Assert-Null (Get-SolutionPackage A 1.0)
    Assert-Null (Get-SolutionPackage B 1.0)
    Assert-Null (Get-SolutionPackage C 1.0)
    Assert-Null (Get-SolutionPackage D 1.0)
}

function Test-UpdatingDependentPackagesPicksLowestCompatiblePackages {
    param(
        $context
    )

    # Arrange
    $p = New-WebApplication
    $p | Install-Package A -Version 1.0 -Source $context.RepositoryPath

    # Act
    Update-Package B -Source $context.RepositoryPath

    # Assert
    Assert-Package $p A 1.5
    Assert-Package $p B 2.0
    Assert-SolutionPackage A 1.5
    Assert-SolutionPackage B 2.0
}