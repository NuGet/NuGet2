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

        $parameters.Remote = $true
        $parameters.AllVersions = $true
        GetPackageVersions $parameters $context 
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

        GetPackageVersions $parameters $context
    }
}

Register-TabExpansion 'Update-Package' @{
    'Id' = {
        param($context)

        $parameters = @{}
        if ($context.id) { $parameters.filter = $context.id }

        GetPackageIds (Find-Package @parameters -Updates -ErrorAction SilentlyContinue)
    }
    'ProjectName' = {
        GetProjectNames
    }
    'Version' = {
        param($context)

        $parameters = @{}
        if ($context.id) { $parameters.filter = $context.id }

        $parameters.Remote = $true
        $parameters.AllVersions = $true
        GetPackageVersions $parameters $context
    }
}

Register-TabExpansion 'Open-PackagePage' @{
    'Id' = {
        param($context)
        GetPackageIds (GetPackages $context)
    }
    'Version' = {
        param($context)

        $parameters = @{}

        if ($context.Id) { $parameters.filter = $context.Id }
        if ($context.Source) { $parameters.source = $context.Source }

        $parameters.Remote = $true
        $parameters.AllVersions = $true
        GetPackageVersions $parameters $context 
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

function GetPackageVersions($parameters, $context) {
    Find-Package @parameters -ExactMatch -ErrorAction SilentlyContinue | Select -ExpandProperty Version | %{
        # Convert to version if the we're looking at the version as a string
        if($_ -is [string]) { 
            [Version]::Parse($_) 
        } else { 
            $_ 
        }  
    } | Sort-Object -Descending
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

function Format-ProjectName {
    param(
        [parameter(position=0, mandatory=$true)]
        [validatenotnull()]
        $Project,
        [parameter(position=1, mandatory=$true)]
        [validaterange(6, 1000)]
        [int]$ColWidth
    )

    # only perform special formatting for web site projects
    if ($project.kind -ne "{E24C65DC-7377-472B-9ABA-BC803B73C61A}") {
        return $project.name
    }

    # less than column width, do nothing
    if ($project.name.length -le $colWidth) {
        return $project.name
    }

    $folder = "\{0}\" -f (split-path -leaf $project.name)
    $root = [io.path]::GetPathRoot($project.name)
    $maxwidth = $colwidth - 6 # len(root + ellipsis)

    # is the directory name too big?
    if ($folder.length -ge $maxwidth) {
        # yes, drop leading backslash and eat into name
        $abbreviated = "{0}...{1}" -f $root, `
            $folder.substring($folder.length - $maxwidth)
    }
    else {
        # no, show like VS solution explorer (drive+ellipsis+end)
        $abbreviated = "{0}...{1}" -f $root, $folder
    }
    
    $abbreviated
}