function Test-RemovingPackageFromProjectDoesNotRemoveIfInUse {
    $p1 = New-ClassLibrary
    $p2 = New-ClassLibrary
    
    Install-Package Ninject -Project $p1.Name
    Assert-References $p1 @("Ninject")
    
    Install-Package Ninject -Project $p2.Name
    Assert-References $p2 @("Ninject")
    
    Uninstall-Package Ninject -Project $p1.Name
    Assert-ReferencesNotExists $p1 @("Ninject")
    Assert-SolutionPackageExists Ninject
}

function Test-RemovePackageRemovesPackageFromSolutionIfNotInUse {
    $p1 = New-WebApplication
    
    Install-Package elmah -Project $p1.Name
    Assert-References $p1 @("elmah")  
    Assert-SolutionPackageExists elmah
    
    Uninstall-Package elmah -Project $p1.Name
    Assert-ReferencesNotExists $p1 @("elmah")
    Assert-SolutionPackageNotExists elmah
}