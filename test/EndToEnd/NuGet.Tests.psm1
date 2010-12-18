# Get the current path and load the functions script
$currentPath = Split-Path $MyInvocation.MyCommand.Definition
$testOutputPath = Join-Path $currentPath "bin"
$templatePath = Join-Path $currentPath "ProjectTemplates"

function New-Guid {
    [System.Guid]::NewGuid().ToString("d").Substring(0, 4)
}

function global:Run-Test {
    param($test)
    
    # Get a reference to the powershell window so we can set focus after the tests are over
    $window = $dte.ActiveWindow
    
    # Close any solution that might be open
    # REVIEW: Should this be a flag?
    if($dte.Solution -and $dte.Solution.IsOpen) {
        $dte.Solution.Close()
    }    
    
    $testRunId = New-Guid
    $testRunOutputPath = Join-Path $testOutputPath $testRunId
    $testRunResultsFile = Join-Path $testRunOutputPath "Results.txt"
    
    mkdir $testRunOutputPath | Out-Null
    
    # Load all of the test scripts
    Get-ChildItem $currentPath -Filter *.ps1 | %{ 
        . $_.FullName $testRunOutputPath $templatePath
    }
    
    $allTests = Get-ChildItem function:\Test*
    
    # If no tests were specified just run all
    if(!$test) {
        $tests = $allTests
    }
    else {
        $tests = @(Get-ChildItem "function:\Test-$test")
        if($tests.Count -eq 0) {
            throw "The test `"$test`" doesn't exist"
        } 
    }    
    
    try {
        # Run all tests
        $tests | %{ 
            try {
                $name = $_.Name.Substring(5)
                
                "Running Test $name..."
                & $_
                Write-Host -ForegroundColor DarkGreen "$name Pass" 
                "$name Pass" >> $testRunResultsFile
            }
            finally {
                # Closed all of the windows after a test runs
                $dte.Documents | %{ $_.Close() }
            }
        }
    }
    catch {
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