$currentPath = Split-Path $MyInvocation.MyCommand.Definition

# Directory where the projects and solutions are created
$testOutputPath = Join-Path $currentPath bin

# Directory where vs templates are located
$templatePath = Join-Path $currentPath ProjectTemplates

# Directory where test scripts are located
$testPath = Join-Path $currentPath tests

$utilityPath = Join-Path $currentPath utility.ps1

# Directory where the test packages are (This is passed to each test method)
$testRepositoryPath = Join-Path $currentPath Packages

# TODO: Add the ability to rerun failed tests from the previous run

function global:Run-Test {
    param(
        [string]$Test,
        [switch]$NewSolution
    )
    
    # Load the utility script since we need to use guid
    . $utilityPath
    
    # Get a reference to the powershell window so we can set focus after the tests are over
    $window = $dte.ActiveWindow
    
    # Close any solution that might be open
    if($dte.Solution -and $dte.Solution.IsOpen) {
        if($NewSolution) {
            $dte.Solution.Close()
        }
    }
    
    $testRunId = New-Guid
    $testRunOutputPath = Join-Path $testOutputPath $testRunId
    $testRunResultsFile = Join-Path $testRunOutputPath "Results.txt"
    
    # Create the output folder
    mkdir $testRunOutputPath | Out-Null
       
    # Load all of the helper scripts from the current location
    Get-ChildItem $currentPath -Filter *.ps1 | %{ 
        . $_.FullName $testRunOutputPath $templatePath
    }
    
    # Load all of the test scripts
    Get-ChildItem $testPath -Filter *.ps1 | %{ 
        . $_.FullName
    } 
    
    # Get all of the the tests functions
    $allTests = Get-ChildItem function:\Test*
    
    # If no tests were specified just run all
    if(!$test) {
        $tests = $allTests
    }
    else {
        $tests = @(Get-ChildItem "function:\Test-$Test")
        
        if($tests.Count -eq 0) {
            throw "The test `"$Test`" doesn't exist"
        } 
    }    
    
    try {
        # Run all tests
        $tests | %{ 
            # Trim the Test- prefix
            $name = $_.Name.Substring(5)
            
            "Running Test $name..."
            
            # REVIEW: We should give the test some context
            # Execute the test passing the repository path
            & $_ $testRepositoryPath
           
            Write-Host -ForegroundColor DarkGreen "Test $name Pass"
            
            "$name Pass" >> $testRunResultsFile        
        }
    }
    catch {
        Write-Host -ForegroundColor Red "Test $name Failed"
        
        "$name Failed: $_" >> $testRunResultsFile        
        throw
    }
    finally {    
        # Deleting tests
        rm function:\Test*
        
        # Set focus back to powershell
        $window.SetFocus()
        
        "Results were written to $testRunResultsFile"
    }
}