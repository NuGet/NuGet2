function Test-UpdatingPackageInProjectDoesntRemoveFromSolutionIfInUse {
    $p1 = New-WebApplication
    $p2 = New-ClassLibrary 

    $antlrOldReferences = @("Antlr3.Runtime", "antlr.runtime", "Antlr3.Utility", "StringTemplate")
    $antlrNewReferences = @("Antlr3.Runtime")

    Install-Package Antlr -Version 3.1.1 -Project $p1.Name
    Assert-References $p1 $antlrOldReferences
    
    Install-Package Antlr -Version 3.1.1 -Project $p2.Name
    Assert-References $p2 $antlrOldReferences
    
    Assert-SolutionPackageExists Antlr 3.1.1
    
    Update-Package Antlr -Project $p1.Name
    Assert-References $p1 $antlrNewReferences
    
    Assert-SolutionPackageExists Antlr 3.1.1
    
    Update-Package Antlr -Project $p2.Name
    Assert-References $p2 $antlrNewReferences
    
    Assert-SolutionPackageNotExists Antlr 3.1.1 
}