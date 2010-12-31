@{

# Script module or binary module file associated with this manifest
ModuleToProcess = 'NuGet.VisualStudio.dll'

# Version number of this module.
ModuleVersion = '1.1'

# ID used to uniquely identify this module
GUID = '76e6f9c4-7045-44c0-a557-17fab0835c12'

# Author of this module
Author = 'NuGet Team'

# Company or vendor of this module
CompanyName = 'Outercurve Foundation'

# Copyright statement for this module
Copyright = '(c) 2010 Outercurve Foundation. All rights reserved.'

# Description of the functionality provided by this module
Description = 'NuGet PowerShell module used for the Package Manager Console'

# Minimum version of the Windows PowerShell engine required by this module
PowerShellVersion = '2.0'

# Name of the Windows PowerShell host required by this module
PowerShellHostName = 'Package Manager Host'

# Minimum version of the Windows PowerShell host required by this module
PowerShellHostVersion = '1.1'

# Minimum version of the .NET Framework required by this module
DotNetFrameworkVersion = '4.0'

# Minimum version of the common language runtime (CLR) required by this module
CLRVersion = ''

# Processor architecture (None, X86, Amd64, IA64) required by this module
ProcessorArchitecture = ''

# Modules that must be imported into the global environment prior to importing this module
RequiredModules = @()

# Assemblies that must be loaded prior to importing this module
RequiredAssemblies = @()

# Script files (.ps1) that are run in the caller's environment prior to importing this module
ScriptsToProcess = @('Scripts\profile.ps1')

# Type files (.ps1xml) to be loaded when importing this module
TypesToProcess = @()

# Format files (.ps1xml) to be loaded when importing this module
FormatsToProcess = @('Scripts\Types.format.ps1xml')

# Modules to import as nested modules of the module specified in ModuleToProcess
NestedModules = @('Scripts\nuget.psm1')

# Functions to export from this module
FunctionsToExport = @('TabExpansion')

# Cmdlets to export from this module
CmdletsToExport = @('Install-Package', 'Find-Package', 'Uninstall-Package', 'Update-Package', 'Get-Package', 'New-Package', 'Get-Project', 'Add-BindingRedirect')

# Variables to export from this module
VariablesToExport = '*'

# Aliases to export from this module
AliasesToExport = '*'

# List of all modules packaged with this module
ModuleList = @()

# List of all files packaged with this module
FileList = @()

# Private data to pass to the module specified in ModuleToProcess
PrivateData = ''

}