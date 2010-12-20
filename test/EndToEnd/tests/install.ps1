function Test-SinglePackageInstallIntoSingleProject {
    # Arrange
    $project = New-ConsoleApplication
    
    # Act
    Install-Package FakeItEasy -Project $project.Name
    
    # Assert
    Assert-Reference $project Castle.Core
    Assert-Reference $project FakeItEasy   
    Assert-Package $project FakeItEasy
    Assert-Package $project Castle.Core
    Assert-SolutionPackage FakeItEasy
    Assert-SolutionPackage Castle.Core
}

function Test-WebsiteSimpleInstall {
    # Arrange
    $p = New-WebSite
    
    # Act
    Install-Package AntiXSS -Project $p.Name
    
    # Assert
    Assert-Package $p AntiXSS
    Assert-SolutionPackage AntiXSS
    Assert-Reference $p AntiXSSLibrary 4.0.0.0
    Assert-Reference $p HtmlSanitizationLibrary 4.0.0.0
}

function Test-DiamondDependencies {
    param(
        [string]$Repository
    )
    
    # Scenario:
    # TestPackageD 1.0 -> TestPackageB 1.0, TestPackageC 1.0
    # TestPackageB 1.0 -> TestPackageA 1.0 
    # TestPackageC 1.0 -> TestPackageA 2.0
    #     TestPackageD 1.0
    #      /            \
    # TestPackageB 1.0 TestPackageC 1.0
    #     |              |
    # TestPackageA 1.0 TestPackageA 2.0
    
    # Arrange 
    $packages = @("TestPackageA", "TestPackageB", "TestPackageC", "TestPackageD")
    $project = New-ClassLibrary
    
    # Act
    Install-Package TestPackageD -Project $project.Name -Source $Repository
    
    # Assert
    $packages | %{ Assert-SolutionPackage $_ }
    $packages | %{ Assert-Package $project $_ }
    $packages | %{ Assert-Reference $project $_ }
    Assert-Package $project TestPackageA 2.0.0.0
    Assert-Reference $project TestPackageA 2.0.0.0
    Assert-Null (Get-ProjectPackage $project TestPackageA 1.0.0.0) 
    Assert-Null (Get-SolutionPackage TestPackageA 1.0.0.0)
}

function Test-WebsiteWillNotDuplicateConfigOnReInstall {
    # Arrange
    $p = New-WebSite
    
    # Act
    Install-Package elmah -Project $p.Name
    $item = Get-ProjectItem $p packages.config
    $item.Delete()
    Install-Package elmah -Project $p.Name
    
    # Assert
    $config = [xml](Get-Content (Get-ProjectItemPath $p web.config))
    Assert-AreEqual 4 $config.configuration.configSections.sectionGroup.section.count
}

function Test-WebsiteConfigElementsAreRemovedEvenIfReordered {
    # Arrange
    $p = New-WebSite
    
    # Act
    Install-Package elmah -Project $p.Name
    $configPath = Get-ProjectItemPath $p web.config
    $config = [xml](Get-Content $configPath)
    $sectionGroup = $config.configuration.configSections.sectionGroup
    $security = $sectionGroup.section[0]
    $sectionGroup.RemoveChild($security) | Out-Null
    $sectionGroup.AppendChild($security) | Out-Null
    $config.Save($configPath)
    Uninstall-Package elmah -Project $p.Name
    $config = [xml](Get-Content $configPath)
    
    # Assert
    Assert-Null $config.configuration.configSections
}