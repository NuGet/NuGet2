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
        GetPackageIds (GetPackages $context)
    }
    'ProjectName' = {
        GetProjectNames
    }
    'Version' = {
        param($context)

        $parameters = @{}

        if ($context.Id) { $parameters.filter = $context.Id }
        if ($context.Source) { $parameters.source = $context.Source }

        GetPackageVersions (Get-Package @parameters -Remote -ErrorAction SilentlyContinue) $context 
    }
}

Register-TabExpansion 'Uninstall-Package' @{
    'Id' = {
        param($context)

        $parameters = @{}
        if ($context.id) { $parameters.filter = $context.id }

        GetPackageIds (Find-Package @parameters -ErrorAction SilentlyContinue)
    }
    'ProjectName' = {
        GetProjectNames
    }
    'Version' = {
        $parameters = @{}
        if ($context.id) { $parameters.filter = $context.id }

        GetPackageVersions (Get-Package @parameters) $context
    }
}

Register-TabExpansion 'Update-Package' @{
    'Id' = {
        param($context)

        $parameters = @{}
        if ($context.id) { $parameters.filter = $context.id }

        GetPackageIds (Find-Package @parameters -ErrorAction SilentlyContinue)
    }
    'ProjectName' = {
        GetProjectNames
    }
    'Version' = {
        param($context)

        $parameters = @{}
        if ($context.id) { $parameters.filter = $context.id }

        GetPackageVersions (Get-Package -Remote @parameters) $context
    }
}

Register-TabExpansion 'New-Package' @{ 'ProjectName' = { GetProjectNames } }
Register-TabExpansion 'Add-BindingRedirect' @{ 'ProjectName' = { GetProjectNames } }
Register-TabExpansion 'Get-Project' @{ 'Name' = { GetProjectNames } }

function GetPackages($context) {  
    $parameters = @{}

    if ($context.Id) { $parameters.filter = $context.Id }
    if ($context.Source) { $parameters.source = $context.Source }

    return Find-Package @parameters -Remote -ErrorAction SilentlyContinue
}

function GetProjectNames {
    $uniqueNames = @(Get-Project -All | Select-Object -ExpandProperty ProjectName)
    
    $simpleNames = Get-Project -All | Select-Object -ExpandProperty Name
    $safeNames = @($simpleNames | Group-Object | Where-Object { $_.Count -eq 1 } | Select-Object -ExpandProperty Name)

    ($uniqueNames + $safeNames) | Select-Object -Unique | Sort-Object
}

function GetPackageIds($packages) {
    $packages | Select -ExpandProperty Id
}

function GetPackageVersions($packages, $context) {
    $packages | Where-Object { $_.Id -eq $context.Id } | Select -ExpandProperty Version | Sort-Object -Descending
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
                # Populate the arguments dictionary with the the name and value of the 
                # associated index. i.e. for the command "Install-Package elmah" arguments should have
                # an entries with { 0, "elmah" } and { "Id", "elmah" }
                $arguments = @{}

                $parsedCommand.Arguments.Keys | Where-Object { $_ -is [int] } | %{
                    $argName = GetArgumentName $command $_
                    $arguments[$argName] = $parsedCommand.Arguments[$_]
                }

                # Copy the arguments over to the parsed command arguments
                $arguments.Keys | %{ 
                    $parsedCommand.Arguments[$_] = $arguments[$_]
                }

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
                    $results = $results | %{ $_.ToString() } | Where-Object { $_.StartsWith($argumentValue, "OrdinalIgnoreCase") }

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