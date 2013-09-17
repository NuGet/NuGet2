param([parameter(Mandatory=$true)][string]$NuGetRoot)

trap
{
    Write-Error -ErrorRecord $_
    ##teamcity[buildStatus status='FAILURE' ]
    exit 1
}

function UpdateVsixManifest($filePath) {	
	[xml]$xml = get-content $filePath
	$root = $xml.Vsix.Identifier

	$vsEditions = $root.SupportedProducts.VisualStudio | % {
		$vsEdition = $_
        
        if ($vsEdition.Version -eq "10.0") {
            $toDelete = $_
        }
        elseif ($vsEdition.Version -eq '11.0') {
            $vsEdition.Version = '12.0'
        }
	}

	if ($toDelete)
	{
		$root.SupportedProducts.removeChild($toDelete)
	}

	$xml.Save($filePath)
}


# Finds and replaces terms in plain text files
function FindReplace([string[]] $files, $findTerm, $replaceTerm, $excludes) {
    if ($excludes) 
    {
        $all = ls -Recurse -I $files $NuGetRoot -Exclude $excludes
    }
    else 
    {
        $all = ls -Recurse -I $files $NuGetRoot
    }
    
	$all | 
	Where { !$_.FullName.Contains(".git") -and $_.Attributes -ne "Directory"} | 
	Where { Select-String -path $_ -pattern $findTerm } | % { 
		Write "Replacing $findTerm with $replaceTerm in $_"
		Set-Content $_ ((Get-Content $_) -Replace $findTerm, $replaceTerm)
	}
}

FindReplace @("*.xaml") "Microsoft.VisualStudio.Shell.10.0" "Microsoft.VisualStudio.Shell.12.0"
FindReplace @("*.vsixmanifest") "NuPackToolsVsix.Microsoft.67e54e40-0ae3-42c5-a949-fddf5739e7a5" "NuGet.67e54e40-0ae3-42c5-a949-fddf5739e6a5"
FindReplace @("*.vsixmanifest") "<Name>NuGet Package Manager</Name>" "<Name>NuGet Package Manager for Visual Studio 2013</Name>"

$vsExtensionDir = "$NuGetRoot\src\VsExtension"
UpdateVsixManifest("$vsExtensionDir\source.extension.vsixmanifest")