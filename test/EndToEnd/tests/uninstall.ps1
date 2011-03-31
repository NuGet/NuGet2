function Test-RemovingPackageFromProjectDoesNotRemoveIfInUse {
    # Arrange
    $p1 = New-ClassLibrary
    $p2 = New-ClassLibrary
    
    Install-Package Ninject -Project $p1.Name
    Assert-Reference $p1 Ninject
    
    Install-Package Ninject -Project $p2.Name
    Assert-Reference $p2 Ninject
    
    Uninstall-Package Ninject -Project $p1.Name
    
    Assert-Null (Get-ProjectPackage $p1 Ninject)
    Assert-Null (Get-AssemblyReference $p1 Ninject)
    Assert-SolutionPackage Ninject
}

function Test-RemovePackageRemovesPackageFromSolutionIfNotInUse {
    # Arrange
    $p1 = New-WebApplication
    
    Install-Package elmah -Project $p1.Name
    Assert-Reference $p1 elmah
    Assert-SolutionPackage elmah
    
    Uninstall-Package elmah -Project $p1.Name
    Assert-Null (Get-AssemblyReference $p1 elmah)
    Assert-Null (Get-ProjectPackage $p1 elmah)
    Assert-Null (Get-SolutionPackage elmah)
}

function Test-UninstallingPackageWithConfigTransformWhenConfigReadOnly {
    # Arrange
    $p1 = New-WebApplication
    
    Install-Package elmah -Project $p1.Name    
    Assert-Reference $p1 elmah
    Assert-SolutionPackage elmah
    attrib +R (Get-ProjectItemPath $p1 web.config)
    
    Uninstall-Package elmah -Project $p1.Name
    Assert-Null (Get-AssemblyReference $p1 elmah)
    Assert-Null (Get-ProjectPackage $p1 elmah)
    Assert-Null (Get-SolutionPackage elmah)
}

function Test-VariablesPassedToUninstallScriptsAreValidWithWebSite {
    param(
        $context
    )
    
    # Arrange
    $p = New-WebSite

    Install-Package PackageWithScripts -Project $p.Name -Source $context.RepositoryRoot

    # This asserts install.ps1 gets called with the correct project reference and package
    Assert-Reference $p System.Windows.Forms

     # Act
    Uninstall-Package PackageWithScripts -Project $p.Name
    Assert-Null (Get-AssemblyReference $p System.Windows.Forms)
}

function Test-UninstallPackageWithNestedContentFiles {
    param(
        $context
    )

    # Arrange
    $p = New-WebApplication
    Install-Package NestedFolders -Project $p.Name -Source $context.RepositoryRoot

    # Act    
    Uninstall-Package NestedFolders -Project $p.Name

    # Assert
    Assert-Null (Get-ProjectItem $p a)
    Assert-Null (Get-ProjectItem $p a\b)
    Assert-Null (Get-ProjectItem $p a\b\c)
    Assert-Null (Get-ProjectItem $p a\b\c\test.txt)
}

function Test-SimpleFSharpUninstall {
    # Arrange
    $p = New-FSharpLibrary
    
    # Act
    Install-Package Ninject -Project $p.Name 
    Assert-Reference $p Ninject
    Assert-Package $p Ninject
    Assert-SolutionPackage Ninject
    Uninstall-Package Ninject -Project $p.Name
    
    # Assert
    Assert-Null (Get-ProjectPackage $p Ninject)
    Assert-Null (Get-AssemblyReference $p Ninject)
    Assert-Null (Get-SolutionPackage Ninject)
}

function Test-UninstallPackageThatIsNotInstalledThrows {
    # Arrange
    $p = New-ClassLibrary

    # Act & Assert
    Assert-Throws { $p | Uninstall-Package elmah } "Unable to find package 'elmah'."
}

function Test-UninstallPackageThatIsInstalledInAnotherProjectThrows {
    # Arrange
    $p1 = New-ClassLibrary
    $p2 = New-ClassLibrary
    $p1 | Install-Package elmah

    # Act & Assert
    Assert-Throws { $p2 | Uninstall-Package elmah } "Unable to find package 'elmah 1.1' in '$($p2.Name)'."
}

function Test-UninstallSolutionOnlyPackage {
    param(
        $context
    )

    # Arrange
    $p = New-MvcApplication
    $p | Install-Package SolutionOnlyPackage -Source $context.RepositoryRoot

    Assert-SolutionPackage SolutionOnlyPackage 2.0

    Uninstall-Package SolutionOnlyPackage

    Assert-Null (Get-SolutionPackage SolutionOnlyPackage 2.0)
}

function Test-UninstallPackageProjectLevelPackageThatsOnlyInstalledAtSolutionLevel {
    # Arrange
    $p = New-ClassLibrary
    $p | Install-Package elmah
    Remove-ProjectItem $p packages.config
    
    Assert-SolutionPackage elmah
    Assert-Null (Get-ProjectPackage $p elmah)

    # Act
    $p | Uninstall-Package elmah

    # Assert
    Assert-Null (Get-SolutionPackage elmah)
}

function Test-UninstallSpecificPackageThrowsIfNotInstalledInProject {
    # Arrange
    $p1 = New-ClassLibrary
    $p2 = New-FSharpLibrary
    $p1 | Install-Package Antlr -Version 3.1.1
    $p2 | Install-Package Antlr -Version 3.1.3.42154

    # Act
    Assert-Throws { $p2 | Uninstall-Package Antlr -Version 3.1.1 } "Unable to find package 'Antlr 3.1.1' in '$($p2.Name)'."
}

function Test-UninstallSpecificVersionOfPackage {
    # Arrange
    $p1 = New-ClassLibrary
    $p2 = New-FSharpLibrary
    $p1 | Install-Package Antlr -Version 3.1.1
    $p2 | Install-Package Antlr -Version 3.1.3.42154

    # Act
    $p1 | Uninstall-Package Antlr -Version 3.1.1

    # Assert
    Assert-Null (Get-ProjectPackage $p1 Antlr 3.1.1)
    Assert-Null (Get-SolutionPackage Antlr 3.1.1)
    Assert-SolutionPackage Antlr 3.1.3.42154
}

function Test-UninstallSpecificVersionOfProjectLevelPackageFromSolutionLevel {        
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
    $p1 | Uninstall-Package Antlr -Version 3.1.1

    # Assert
    Assert-Null (Get-SolutionPackage Antlr 3.1.1)
    Assert-NotNull (Get-SolutionPackage Antlr 3.1.3.42154)
}

function Test-UninstallAmbiguousProjectLevelPackageFromSolutionLevel {    
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
    Assert-Throws { $p1 | Uninstall-Package Antlr } "Unable to find 'Antlr' in '$($p1.Name)' and found multiple versions of 'Antlr' installed. Please specify a version."
}

function Test-UninstallSolutionOnlyPackageWhenAmbiguous {
    param(
        $context
    )

    # Arrange
    $p = New-MvcApplication
    Install-Package SolutionOnlyPackage -Version 1.0 -Source $context.RepositoryRoot
    Install-Package SolutionOnlyPackage -Version 2.0 -Source $context.RepositoryRoot

    Assert-SolutionPackage SolutionOnlyPackage 1.0
    Assert-SolutionPackage SolutionOnlyPackage 2.0

    Assert-Throws { Uninstall-Package SolutionOnlyPackage } "Found multiple versions of 'SolutionOnlyPackage' installed. Please specify a version."
}

function Test-UninstallPackageWorksWithPackagesHavingSameNames {
    #
    #  Folder1
    #     + ProjectA
    #     + ProjectB
    #  Folder2
    #     + ProjectA
    #     + ProjectC
    #  ProjectA
    #

    # Arrange
    $f = New-SolutionFolder 'Folder1'
    $p1 = $f | New-ClassLibrary 'ProjectA'
    $p2 = $f | New-ClassLibrary 'ProjectB'

    $g = New-SolutionFolder 'Folder2'
    $p3 = $g | New-ClassLibrary 'ProjectA'
    $p4 = $g | New-ConsoleApplication 'ProjectC'

    $p5 = New-ConsoleApplication 'ProjectA'

    # Act
    Get-Project -All | Install-Package elmah
    $all = @( $p1, $p2, $p3, $p4, $p5 )
    $all | % { Assert-Package $_ elmah }

    Get-Project -All | Uninstall-Package elmah

    # Assert
    $all | % { Assert-Null (Get-ProjectPackage $_ elmah) }
}

function Test-UninstallPackageWithXmlTransformAndTokenReplacement {
    param(
        $context
    )

    # Arrange
    $p = New-WebApplication
    $p | Install-Package PackageWithXmlTransformAndTokenReplacement -Source $context.RepositoryRoot

    # Assert
    $ns = $p.Properties.Item("DefaultNamespace").Value
    $assemblyName = $p.Properties.Item("AssemblyName").Value
    $path = (Get-ProjectItemPath $p web.config)
    $content = [System.IO.File]::ReadAllText($path)
    $expectedContent = "type=`"$ns.MyModule, $assemblyName`""
    Assert-True ($content.Contains($expectedContent))

    # Act
    $p | Uninstall-Package PackageWithXmlTransformAndTokenReplacement
    $content = [System.IO.File]::ReadAllText($path)
    Assert-False ($content.Contains($expectedContent))
}

function Test-UninstallPackageAfterRenaming {
    param(
        $context
    )
    # Arrange
    $f = New-SolutionFolder 'Folder1' | New-SolutionFolder 'Folder2'
    $p0 = New-ClassLibrary 'ProjectX'
    $p1 = $f | New-ClassLibrary 'ProjectA'
    $p2 = $f | New-ClassLibrary 'ProjectB'

    # Act
    $p1 | Install-Package NestedFolders -Source $context.RepositoryRoot 
    $p1.Name = "ProjectX"
    Uninstall-Package NestedFolders -Project Folder1\Folder2\ProjectX

    $p2 | Install-Package NestedFolders -Source $context.RepositoryRoot 
    $f.Name = "Folder3"
    Uninstall-Package NestedFolders -Project Folder1\Folder3\ProjectB

    Assert-Null (Get-ProjectItem $p1 scripts\jquery-1.5.js)
    Assert-Null (Get-ProjectItem $p2 scripts\jquery-1.5.js)
}