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

    Assert-Throws { $p | Update-Package C -Source $context.RepositoryPath } "Conflict occurred. 'C 1.0' referenced but requested 'C 2.0'. 'G 1.0' depends on 'C 1.0'."
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
