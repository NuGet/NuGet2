function Test-PackFromProject {
    param(
        $context
    )

    $p = New-ClassLibrary
    $p.Properties.Item("Company").Value = "Some Company"
    $item = Get-ProjectItem $p Properties\AssemblyInfo.cs
    $item.Save()
    $p.Save()

    $output = (Get-PropertyValue $p FullPath)
    & $context.NuGetExe pack $p.FullName -build -o $output

    $packageFile = Get-ChildItem $output -Filter *.nupkg
    Assert-NotNull $packageFile
    $zipPackage = New-Object NuGet.ZipPackage($packageFile.FullName)
    Assert-AreEqual $p.Name $zipPackage.Id
    Assert-AreEqual '1.0' $zipPackage.Version.ToString()
    Assert-AreEqual 'Some Company' $zipPackage.Authors
    Assert-AreEqual 'Description' $zipPackage.Description
    $files = @($zipPackage.GetFiles())
    Assert-AreEqual 1 $files.Count
    Assert-AreEqual "lib\net40\$($p.Name).dll" $files[0].Path
    $assemblies = @($zipPackage.AssemblyReferences)
    Assert-AreEqual 1 $assemblies.Count
    Assert-AreEqual "$($p.Name).dll" $assemblies[0].Name
}

function Test-PackFromProjectUsesInstalledPackagesAsDependencies {
    param(
        $context
    )

    $p = New-ClassLibrary
    
    $p | Install-Package PackageWithContentFileAndDependency -Source $context.RepositoryRoot
    $p.Save()

    $output = (Get-PropertyValue $p FullPath)
    & $context.NuGetExe pack $p.FullName -build -o $output

    $packageFile = Get-ChildItem $output -Filter *.nupkg
    Assert-NotNull $packageFile
    $zipPackage = New-Object NuGet.ZipPackage($packageFile.FullName)
    $dependencies = @($zipPackage.Dependencies)

    Assert-NotNull $dependencies
    Assert-AreEqual 1 $dependencies.Count
    Assert-AreEqual 'PackageWithContentFileAndDependency' $dependencies[0].Id
    Assert-AreEqual '1.0' $dependencies[0].VersionSpec.ToString()
}

function Test-PackFromProjectUsesVersionSpecForDependencyIfApplicable {
    $p = New-ClassLibrary
    
    $p | Install-Package PackageWithContentFileAndDependency -Source $context.RepositoryRoot
    Add-PackageConstraint $p PackageWithContentFileAndDependency "[1.0, 2.5)"
    $p.Save()

    $output = (Get-PropertyValue $p FullPath)
    & $context.NuGetExe pack $p.FullName -build -o $output

    $packageFile = Get-ChildItem $output -Filter *.nupkg
    Assert-NotNull $packageFile
    $zipPackage = New-Object NuGet.ZipPackage($packageFile.FullName)
    $dependencies = @($zipPackage.Dependencies)

    Assert-NotNull $dependencies
    Assert-AreEqual 1 $dependencies.Count
    Assert-AreEqual 'PackageWithContentFileAndDependency' $dependencies[0].Id
    Assert-AreEqual "[1.0, 2.5)" $dependencies[0].VersionSpec.ToString()
}