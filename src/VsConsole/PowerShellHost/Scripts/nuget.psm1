# make sure we stop on exceptions
$ErrorActionPreference = "Stop"

# This object reprents the result value for tab expansion functions when no result is returned.
# This is so that we can distinguish it from $null, which has different semantics
$NoResultValue = New-Object PSObject -Property @{ NoResult = $true }

# Hashtable that stores tab expansion definitions
$TabExpansionCommands = @{}

# This function allows 3rd parties to enable intellisense for arbitrary functions
function Register-TabExpansion {
    [CmdletBinding()]
    param(
        [parameter(Mandatory = $true)]
        [string]$Name,
        [parameter(Mandatory = $true)]
        $Definition
    )
 
    $TabExpansionCommands[$Name] = $Definition 
}

Register-TabExpansion 'Install-Package' @{
    'Id' = {
        param($context)
        $filter = $context.Id
        $source = $context.Source
 
        if($source) {
            $packages = Find-Package -Remote -Source $source -Filter $filter -ea 'SilentlyContinue'
        }
        else {
            $packages = Find-Package -Remote -Filter $filter -ea 'SilentlyContinue'
        }

        NormalizePackages $packages
    }
    'Project' = {
        GetProjectNames
    }
}


$localPackagesTabExpansion = @{
    'Id' = {
        param($context)        
        NormalizePackages (Find-Package -Filter $context.Id -ea 'SilentlyContinue')
    }
    'Project' = {
        GetProjectNames
    }
}

Register-TabExpansion 'Uninstall-Package' $localPackagesTabExpansion
Register-TabExpansion 'Update-Package' $localPackagesTabExpansion
Register-TabExpansion 'New-Package' @{ 'Project' = { GetProjectNames } }
Register-TabExpansion 'Get-Project' @{ 'Name' = { GetProjectNames } }


function GetProjectNames {
    Get-Project -All | Select -ExpandProperty Name
}

function NormalizePackages($packages) {
    $packages | Group-Object Id | Select -ExpandProperty Name
}

function NugetTabExpansion($line, $lastWord) {
    # Parse the command
    $parsedCommand = [NuGetConsole.Host.PowerShell.CommandParser]::Parse($line)

    # Get the command definition
    $definition = $TabExpansionCommands[$parsedCommand.CommandName]

    # See if we've registered a command for intellisense
    if($definition) {
        # Get the command that we're trying to show intellisense for
        $command = Get-Command $parsedCommand.CommandName -ErrorAction SilentlyContinue

        if($command) {
            # We're trying to find out what parameter we're trying to show intellisense for based on 
            # either the name of the an argument or index e.g. "Install-Package -Id " "Install-Package "
            
            $argument = $parsedCommand.CompletionArgument
            $index = $parsedCommand.CompletionIndex

            if(!$argument -and $index -ne $null) {                
                do {
                    # Get the argument name for this index
                    $argument = GetArgumentName $command $index

                    if(!$argument) {
                        break
                    }
                    
                    # If there is already a value for this argument, then check the next one index.
                    # This is so we don't show duplicate intellisense e.g. "Install-Package -Id elmah {tab}".
                    # The above statement shouldn't show intellisense for id since it already has a value
                    if($parsedCommand.Arguments[$argument] -eq $null) {
                        $value = $parsedCommand.Arguments[$index]
                        if(!$value) {
                            $value = ''   
                        }
                        $parsedCommand.Arguments[$argument] = $value
                        break
                    }
                    else {
                        $index++
                    }

                } while($true);
                
            }

            if($argument) {
                # If the argument is a true argument of this command and not a partial argument
                # and there is a non null value (empty is valid), then we execute the script block
                # for this parameter (if specified)
                $action = $definition[$argument]
                $argumentValue = $parsedCommand.Arguments[$argument]
                        
                if($command.Parameters[$argument] -and 
                   $argumentValue -ne $null -and
                   $action) {
                    $context = New-Object PSObject -Property $parsedCommand.Arguments

                    $results = @(& $action $context)

                    if($results.Count -eq 0) {
                        return $null
                    }

                    # Use the argument value to filter results
                    $results = $results | Where-Object { $_.StartsWith($argumentValue, "OrdinalIgnoreCase") } | Sort-Object

                    return NormalizeResults $results
                }
            }
        }
    } 

    return $NoResultValue
}

function NormalizeResults($results) {
    $results | %{
        $result = $_

        # Add quotes to a result if it contains whitespace or a quote
        $addQuotes = $result.Contains(" ") -or $result.Contains("'") -or $result.Contains("`t")
        
        if($addQuotes) {
            $result = "'" + $result.Replace("'", "''") + "'"
        }

        return $result
    }
}

function GetArgumentName($command, $index) {    
    # Next we try to find the parameter name for the parameter index (in the default parameter set)
    $parameterSet = $Command.DefaultParameterSet

    if(!$parameterSet) {
        $parameterSet = '__AllParameterSets'
    }

    return $command.Parameters.Values | ?{ $_.ParameterSets[$parameterSet].Position -eq $index } | Select -ExpandProperty Name
}

# Hook up Solution events

$solutionEvents = Get-Interface $dte.Events.SolutionEvents ([EnvDTE._dispSolutionEvents_Event])

$solutionEvents.add_Opened([EnvDTE._dispSolutionEvents_OpenedEventHandler]{
    UpdateWorkingDirectory
})

$solutionEvents.add_AfterClosing([EnvDTE._dispSolutionEvents_AfterClosingEventHandler]{
    UpdateWorkingDirectory
})

function UpdateWorkingDirectory {
    $SolutionDir = if($DTE -and $DTE.Solution -and $DTE.Solution.FullName) { Split-Path $DTE.Solution.FullName -Parent }
    if ($SolutionDir) {
        Set-Location $SolutionDir
    } 
    else {
        Set-Location $Env:USERPROFILE
    }
}

function IsSolutionOpen() {
   return ($dte -and $dte.Solution -and $dte.Solution.IsOpen)
}

if (IsSolutionOpen) {
    UpdateWorkingDirectory
}