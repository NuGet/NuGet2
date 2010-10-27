param($installPath, $toolsPath, $package, $project)

$requiredAssemblies = Get-ChildItem $toolsPath -Filter *.dll

# Add a reference to the required assemblies
$requiredAssemblies | ForEach-Object { Add-Type -Path $_.FullName }

$global:mvcScaffoldToolsPath = $toolsPath
$global:mvcViewTemplatesPath = Join-Path $env:VS100COMNTOOLS "..\IDE\ItemTemplates\CSharp\Web\MVC 2\CodeTemplates\AddView"

function global:Add-MvcView {
    [CmdletBinding()]
    param(        
        [parameter(Mandatory = $true, ValueFromPipelineByPropertyName = $true)]
        [string]$TemplateName,

        [string]$Name,

        [string]$OutputPath,

        [string]$ModelType,
        
        [string]$Project,

        [string]$OutputFileExtension = ".aspx",

        [string]$AreaName,

        [switch]$ContentPage,

        [switch]$Partial,

        [string]$MasterPageFile,

        [string]$Namespace
    )
    Process {
        if(!$Name) {
            # Default name to the template name
            $Name = $TemplateName
        }

        $viewTemplates = Get-MvcViewTemplates $Project

        # Create an mvc template host wit the properties
        $mvcHost = New-Object MvcToolsShim.MvcExtendedHost
        $mvcHost.TemplateFile = $viewTemplates["$TemplateName.tt"]
        $mvcHost.ViewName = $Name        
        $mvcHost.AreaName = $AreaName
        $mvcHost.IsContentPage = $ContentPage
        $mvcHost.IsPartialView = $Partial
        $mvcHost.MasterPageFile = $MasterPageFile
        $mvcHost.Namespace = $Namespace
        $mvcHost.OutputFileExtension = $OutputFileExtension
        $mvcHost.ViewDataTypeName = $ModelType
        $mvcHost.AssemblyPath = New-Object "System.Collections.Generic.List``1[System.String]"

        if(!$mvcHost.TemplateFile) {
            throw "Unable to find template '$TemplateName'"
        }
        
        # Add required assemblies to the path
        $requiredAssemblies = Get-ChildItem $global:mvcScaffoldToolsPath -Filter *.dll
        $requiredAssemblies | ForEach-Object { 
            $mvcHost.AssemblyPath.Add($_.FullName) 
        }
        
        $vsProject = Get-Project $Project
        
        if($ModelType) {
            # Get the types in the project
            $modelTypes = [Microsoft.VisualStudio.Helpers.VsHelper]::GetAvailableTypes($vsProject, $false)

            $type = $modelTypes[$ModelType]

            # We didn't find an exact match so try to do a partial match
            if(!$type) {
                if(!$ModelType.Contains(".")) {
                    $matchType = ".$ModelType"
                }
                else {
                    $matchType = $ModelType
                }

                # Try to find all keys that end with this type name
                $matchingKeys = @($modelTypes.Keys | Where-Object { $_.EndsWith($matchType, [StringComparison]"OrdinalIgnoreCase") })
                
                if($matchingKeys.Length -eq 0) {
                    throw "Unknown type '$ModelType'"
                }

                if($matchingKeys.Length -gt 1) {
                    throw "Ambiguous type name '$ModelType', try specifying the full type name"
                }

                $type = $modelTypes[$matchingKeys[0]]
            }
                        
            # Try to find the type for the type name
            $mvcHost.ViewDataType = $type
            $mvcHost.ViewDataTypeName = $type.get_FullName()

            # Add the assembly path to the list of paths in the t4 references
            $mvcHost.AssemblyPath.Add($mvcHost.ViewDataType.Assembly.Location)
        }

        # Loop over all the references and add them to the host in case the t4 needs them
        foreach($reference in $vsProject.Object.References) {
            # If an assembly reference is unresolved (e.g. if user deletes the referenced assembly)
            # the VS project will list the path as an empty string, so we will filter it out
            if ($reference.Path) {                
                $mvcHost.AssemblyPath.Add($reference.Path);
            }
        }

        if(!$mvcHost.Namespace) {
            # No namespace specified so use the default namespace
            $mvcHost.Namespace = $vsProject.Properties.Item("DefaultNamespace").Value
        }

        # Get the framework moniker from the project properties
        $frameworkMoniker = $vsProject.Properties.Item("TargetFrameworkMoniker").Value
        
        # Get the framework name and version
        $frameworkName = New-Object System.Runtime.Versioning.FrameworkName($frameworkMoniker)
        $mvcHost.FrameworkVersion = $frameworkName.Version

        # Process the template
        $output = [MvcToolsShim.MvcExtendedHost]::ProcessTemplate($mvcHost)
        
        if($mvcHost.Errors.HasErrors) {
            $mvcHost.Errors | Sort-Object Line | Sort-Object Column
        }
        else {            
            if($ModelType) {
                $OutputPath = Join-Path $ModelType $OutputPath
            }

            $OutputPath = Join-Path Views $OutputPath

            if($AreaName) {
                # If an area name was specified, then use it
                $OutputPath = Join-Path (Join-Path Areas $AreaName) $OutputPath
            }

            # Get the project item for the output path
            $projectItem = Get-ProjectFolder $OutputPath -Create -Project $Project

            # Get a temp file to store the output of the t4
            $tempFile = Join-Path $env:temp $Name
            $tempFile += $OutputFileExtension

            # Write the output file
            $output | Out-File $tempFile
            
            # If the item exists then delete it
            try {
                $fileItem = $projectItem.Item("$Name$OutputFileExtension")
                $fileItem.Delete()
            }
            catch {
                # Doesn't exist
            }            

            # Add the file to the project
            $projectItem.AddFromFileCopy($tempFile) | Out-Null

            # Remove the temp file
            Remove-Item $tempFile -Force

            $outPath = Join-Path $OutputPath $Name
            Write-Host "Added file '$outPath$OutputFileExtension'"                        
        }
    }
}

function global:Scaffold-MvcViews {
    [CmdletBinding()]
    param (
        [parameter(Mandatory = $true, ValueFromPipelineByPropertyName = $true)]
        [string]$ModelType,

        [string]$OutputPath,

        [string]$Project,

        [string]$AreaName
    )
    Process {        
        # TODO: Handle in project override
        # Default template names
        $templateNames = @("List", "Details", "Edit", "Create", "Delete")
        
        # Add a view for each of these templates
        $templateNames | ForEach-Object { Add-MvcView -TemplateName $_ -OutputPath $OutputPath -ModelType $ModelType -AreaName $AreaName -Project $Project }
    }
}

function global:Get-MvcViewTemplates {
    [CmdletBinding()]
    param (                
        [string]$Project
    )

    $templates = @{}

    Get-ChildItem $global:mvcViewTemplatesPath | ForEach-Object { $templates[$_.Name] = $_.FullName }

    $viewsTemplateFolder = Get-ProjectFolder "CodeTemplates\AddView" -Project $Project
    if($viewsTemplateFolder) {
        $viewsTemplateFolder | ForEach-Object { $templates[$_.Name] = $_.Properties.Item("FullPath").Value }
    }

    return $templates
}

# Utility functions

function global:Get-ProjectItem {
    [CmdletBinding()]
    param(
        [parameter(Mandatory = $true, ValueFromPipelineByPropertyName = $true)]
        [string]$Path,

        [string]$Project
    )

    $folderPath = [System.IO.Path]::GetDirectoryName($Path)

    if($folderPath -eq "") {
        return $null
    }

    $fileName = [System.IO.Path]::GetFileName($Path)

    $container = Get-ProjectFolder -Path $folderPath -Project $Project

    if(!$container) {
        return $null
    }

    try {
        return $container.Item($fileName)
    }
    catch {
        return $null
    }
}

function global:Get-ProjectFolder {
    [CmdletBinding()]
    param(
        [parameter(Mandatory = $true, ValueFromPipelineByPropertyName = $true)]
        [string]$Path,

        [switch]$Create,

        [string]$Project
    )
    Process {        
        $vsProject = Get-Project $Project

        $pathParts = $path.Split('\')
        $projectItems = $vsProject.ProjectItems
        
        foreach($folder in $pathParts) {
            if(!$folder -or $folder -eq "") {
                continue
            }

            try {
                $subFolder = $projectItems.Item($folder)
            }
            catch {
                if(!$Create) {
                    return $null
                }

                # Get the full path property
                $property = $projectItems.Parent.Properties.Item("FullPath")

                # Get the full path of this folder
                $fullPath = Join-Path ($property.Value) $folder

                # Create the folder on disk first
                mkdir $fullPath | Out-Null

                $subFolder = $projectItems.AddFromDirectory($fullPath)
            }

            $projectItems = $subFolder.ProjectItems
        }

        # We don't want powershell to implicitly convert projectitems to an array
        ,$projectItems
    }
}
