param([string]$outputPath)

# Make sure we stop on exceptions
$ErrorActionPreference = "Stop"

function Ensure-Solution {
    if(!$dte.Solution -or !$dte.Solution.IsOpen) {
        New-Solution
    }
}

function Ensure-Dir {
    param([string]$path)
    if(!(Test-Path $path)) {
        mkdir $path | Out-Null
    }
}

function New-Solution {
    $id = New-Guid
    $name = "Solution_$id"
    $solutionDir = (Join-Path $outputPath $name)
    $solutionPath = Join-Path $solutionDir $name    
    
    Ensure-Dir $solutionDir
     
    $dte.Solution.Create($solutionDir, $name) | Out-Null
    $dte.Solution.SaveAs($solutionPath) | Out-Null
}

function New-Project {
    param(
         [string]$name,
         [string]$lang = "CSharp"
    )
    
    $id = New-Guid
    $projectName = $name + "_$id"
    
    Ensure-Solution
    $templatePath = $dte.Solution.GetProjectTemplate("$name.zip", $lang)
    $solutionDir = Split-Path $dte.Solution.FullName
    $destPath = Join-Path $solutionDir $projectName
    
    $dte.Solution.AddFromTemplate($templatePath, $destPath, $projectName) | Out-Null
    Get-Project $projectName
}

function New-ClassLibrary { 
    New-Project ClassLibrary
}

function New-ConsoleApplication {
    New-Project ConsoleApplication
}

function New-WebApplication {
    New-Project EmptyWebApplicationProject40
}

function New-MvcApplication { 
    New-Project EmptyMvcWebApplicationProjectTemplatev2.0.cs
}

function Assert-PackagesConfig {
    param($project)
    try {
        $project.ProjectItems.Item("packages.config") | Out-Null
    }
    catch {
        Assert-Fail "packages.config doesn't exist for project $($project.Name)"
    }
}

function Assert-PackagesDir {
    Assert-PathExists (Get-PackagesDir) "The packages directory doesn't exist"
}

function Assert-ReferencesNotExists {
    param(
         $project, 
         $references
    ) 
    
    $references | %{ 
        $reference = $_
        try {
            # TODO: Handle Websites
            $ref = $project.Object.References.Item($_)
            
        }
        catch {
            
        }
        
        if($ref) {
            Assert-Fail "Reference `"$reference`" exists in project $($project.Name)"
        }
    }
}

function Assert-References {
    param(
         $project, 
         $references
    ) 
    
    $references | %{ 
        $reference = $_
        try {
            # TODO: Handle Websites
            $ref = $project.Object.References.Item($_)
            
            if(!$ref -or !$ref.Path) {
                throw
            }
        }
        catch {
            Assert-Fail "Reference `"$reference`" doesn't exist or is broken in project $($project.Name)"
        }
    }
}

function Assert-SolutionPackageExists {
    param(
          $id, 
          $version
    )
    # Make sure the packages directory exists
    Assert-PackagesDir
    
    $packages = _Get-Package $id $version
    
    if($packages.Count -eq 0) {
        Assert-Fail "Package $id $version does not exist at solution level"
    }
}

function Assert-SolutionPackageNotExists {
    param(
          $id, 
          $version
    )
    
    $packages = _Get-Package $id $version
    
    if($packages.Count -gt 0) {
        Assert-Fail "Package $id $version exists at solution level"
    }
}

function _Get-Package {
    param(
          $name, 
          $version
    )
    
    # Get the package entries from the solution
    $packages = @(List-Package | ?{ $_.Id -eq $name -and (($version -and $version.StartsWith($_.Version)) -or !$version) })
    
    # Force an array return value
    ,$packages
}

function Assert-PathExists {
    param(
          [string]$path, 
          $message
    )
    
    if(!(Test-Path $path)) {
        if(!$message) {
            $message = "Path `"$path`" does not exist"
        }
        Assert-Fail $message
    }
}

function Get-PackagesDir {
    # TODO: Handle when the package location changes
    $solutionDir = Split-Path $dte.Solution.FullName
    Join-Path $solutionDir "packages"
}

function Assert-Fail {
    param($message)
    throw "ASSERT: $message"
}