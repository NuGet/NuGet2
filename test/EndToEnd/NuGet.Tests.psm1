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

$nugetRoot = Join-Path $currentPath "..\.."

$toolsPath = "$nugetRoot\Tools"

$generatePackagesProject = Join-Path $toolsPath NuGet\GenerateTestPackages\GenerateTestPackages.csproj

$generatePackagesExePath = Join-Path $toolsPath NuGet\GenerateTestPackages\bin\Debug\GenerateTestPackages.exe

$nugetExePath = "$nugetRoot\src\CommandLine\bin\Debug\NuGet.exe"

$msbuildPath = Join-Path $env:windir Microsoft.NET\Framework\v4.0.30319\msbuild

# TODO: Add the ability to rerun failed tests from the previous run

# Add intellisense for the test parameter
Register-TabExpansion 'Run-Test' @{
    'Test' = { 
        # Load all of the test scripts
        Get-ChildItem $testPath -Filter *.ps1 | %{ 
            . $_.FullName
        }
    
        # Get all of the tests functions
        Get-ChildItem function:\Test* | %{ $_.Name.Substring(5) }
    }
    'File' = { 
        # Get all of the tests files
        Get-ChildItem $testPath -Filter *.ps1 | Select-Object -ExpandProperty Name
    }
}

function global:Run-Test {
    [CmdletBinding(DefaultParameterSetName="Test")]
    param(
        [parameter(ParameterSetName="Test", Position=0)]
        [string]$Test,
        [parameter(ParameterSetName="File", Mandatory=$true, Position=0)]
        [string]$File,
        [parameter(Position=1)]
        [bool]$LaunchResultsOnFailure=$true
    )
    
    if(!(Test-Path $generatePackagesExePath)) {
        & $msbuildPath $generatePackagesProject /v:quiet
    }
    
    # Close the solution after every test run
    $dte.Solution.Close()
    
    # Load the utility script since we need to use guid
    . $utilityPath
    
    # Get a reference to the powershell window so we can set focus after the tests are over
    $window = $dte.ActiveWindow
    
    $testRunId = New-Guid
    $testRunOutputPath = Join-Path $testOutputPath $testRunId
    $testRunResultsFile = Join-Path $testRunOutputPath Results.html
    $testLogFile = Join-Path $testRunOutputPath log.txt
    
    # Create the output folder
    mkdir $testRunOutputPath | Out-Null
       
    # Load all of the helper scripts from the current location
    Get-ChildItem $currentPath -Filter *.ps1 | %{ 
        . $_.FullName $testRunOutputPath $templatePath
    }
    
    Write-Verbose "Loading scripts from `"$testPath`""
    
    if (!$File) {
        $File = "*.ps1"
    }

    # Load all of the test scripts
    Get-ChildItem $testPath -Filter $File | %{ 
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
    
    $results = @()
    
    # Add a reference to the msbuild assembly in case it isn't there
    Add-Type -AssemblyName "Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"

    # The vshost that VS launches caues the functional tests to freeze sometimes so disable it
    [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.SetGlobalProperty("UseVSHostingProcess", "false")
    
    try {
        # Run all tests
        $tests | %{ 
            # Trim the Test- prefix
            $name = $_.Name.Substring(5)
            
            "Running Test $name..."

            # Write to log file as we run tests
            "Running Test $name..." >> $testLogFile
            
            $repositoryPath = Join-Path $testRepositoryPath $name

            $values = @{
                RepositoryRoot = $testRepositoryPath
                TestRoot = $repositoryPath
                RepositoryPath = Join-Path $repositoryPath Packages
                NuGetExe = $nugetExePath
            }
            
            if(Test-Path $repositoryPath) {            
                pushd 
                Set-Location $repositoryPath
                # Generate any packages that might be in the repository dir
				Get-ChildItem $repositoryPath\* -Include *.dgml,*.nuspec | %{
                    & $generatePackagesExePath $_.FullName | Out-Null
                } 
                popd
            }
            
            $context = New-Object PSObject -Property $values

            try {
                & $_ $context
                
                Write-Host -ForegroundColor DarkGreen "Test $name Passed"
                
                $results += New-Object PSObject -Property @{ 
                    Test = $name
                    Error = $null
                }
            }
            catch {                     
                if($_.Exception.Message.StartsWith("SKIP")) {
                    $message = $_.Exception.Message.Substring(5).Trim()
                    $results += New-Object PSObject -Property @{ 
                        Test = $name
                        Error = $message
                        Skipped = $true
                    }

                    Write-Warning "$name was Skipped: $message"
                }
                else {                    
                    $results += New-Object PSObject -Property @{ 
                        Test = $name
                        Error = $_
                    }
                    Write-Host -ForegroundColor Red "$($_.InvocationInfo.InvocationName) Failed: $_"
                }
            }
            finally {
                try {           
                    # Clear the cache after running each test
                    [NuGet.MachineCache]::Default.Clear()
                }
                catch {
                    # The type might not be loaded so don't fail if it isn't
                }

                if($tests.Count -gt 1) {
                    $dte.Solution.Close()
                }
         
                # Cleanup the output from running the generate packages tool
                Remove-Item (Join-Path $repositoryPath Packages) -Force -Recurse -ErrorAction SilentlyContinue
                Remove-Item (Join-Path $repositoryPath Assemblies) -Force -Recurse -ErrorAction SilentlyContinue
            }
        }
    }
    finally {        
        # Deleting tests
        rm function:\Test*
        
        # Set focus back to powershell
        $window.SetFocus()              
               
        Write-TestResults $testRunId $results $testRunResultsFile $LaunchResultsOnFailure

        # Clear out the setting when the tests are done running
        [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.SetGlobalProperty("UseVSHostingProcess", "")
    }
}

function Write-TestResults {
    param(
        $TestRunId,
        $Results,
        $Path,
        $LaunchResultsOnFailure
    )

    # Show failed tests first
    $Results = $Results | Sort-Object -Property Error -Descending
    
    $resultsTemplate = "<html>
    <head>
        <title>
            Test run {0} results
        </title>
        <style>
            
        body
        {{
            font-family: Trebuchet MS;
            font-size: 0.80em;
            color: #000;
        }}

        a:link, a:visited
        {{
            text-decoration: none;
        }}

        p, ul
        {{
            margin-bottom: 20px;
            line-height: 1.6em;
        }}

        h1, h2, h3, h4, h5, h6
        {{
            font-size: 1.5em;
            color: #000;
            font-family: Arial, Helvetica, sans-serif;
        }}

        h1
        {{
            font-size: 1.8em;
            padding-bottom: 0;
            margin-bottom: 0;
        }}
        h2
        {{
            padding: 0 0 10px 0;
        }}  
        table
        {{
            width: 90%;
            border-collapse:collapse;
        }}
        table td
        {{
            padding: 4px;
            border:1px solid #CCC;
        }}
        table th 
        {{
            text-align:left;
            border:1px solid #CCC;
        }}
        .Skipped 
        {{
            color:black;
            background-color:Yellow;
            font-weight:bold;
        }}
        .Passed 
        {{
        }}
        .Failed
        {{
            color:White;
            background-color:Red;
            font-weight:bold;
        }}
        </style>
    </head>
    <body>
        <h2>Test Run {0} ({1})</h2>
        <h3>Ran {2} Tests, {3} Passed, {4} Failed, {5} Skipped</h3>
        <table>
            <tr>
                <th>
                    Result
                </th>
                <th>
                    Test Name
                </th>
                <th>
                    Error Message
                </th>
            </tr>
            {6}
            </table>
    </body>
</html>";

    $testTemplate = "<tr>
    <td class=`"{0}`">{0}</td>
    <td class=`"{0}`">{1}</td>
    <td class=`"{0}`">{2}</td>
    </tr>"
    
    $pass = 0
    $fail = 0
    $skipped = 0

    $rows = $Results | % { 
        $status = 'Passed'
        if($_.Skipped) {
            $status = 'Skipped'
            $skipped++
        }
        elseif($_.Error) {
            $status = 'Failed'
            $fail++
        }
        else {
            $pass++
        }
        
        [String]::Format($testTemplate, $status, 
                         [System.Net.WebUtility]::HtmlEncode($_.Test), 
                         [System.Net.WebUtility]::HtmlEncode($_.Error))
    }

    [String]::Format($resultsTemplate, $TestRunId, (Split-Path $Path), $Results.Count, $pass, $fail, $skipped, [String]::Join("", $rows)) | Out-File $Path | Out-Null
    Write-Host "Ran $($Results.Count) Tests, $pass Passed, $fail Failed, $skipped Skipped. See $Path for more details"

    if (($fail -gt 0) -and $LaunchResultsOnFailure -and ($Results.Count -gt 1)) 
    {
        [System.Diagnostics.Process]::Start($Path)
    }
}

function Get-PackageRepository
{
    param
    (
        $source
    )

    $componentModel = Get-VSComponentModel
    $repositoryFactory = $componentModel.GetService([NuGet.IPackageRepositoryFactory])
    $repositoryFactory.CreateRepository($source)
}