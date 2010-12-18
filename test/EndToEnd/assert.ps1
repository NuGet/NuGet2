# Assert functions
function Build-ErrorMessage {
    param(
        [parameter(Mandatory = $true)]
        [string]$BaseMessage,
        [string]$Message
    )
    
    if($Message) {
        $BaseMessage += ". $Message"
    }
    
    $BaseMessage
}

function Get-AssertError {
    param(
        [parameter(Mandatory = $true)]
        [string]$BaseMessage,
        [string]$Message
    )
    
    $Message = Build-ErrorMessage $BaseMessage $Message
        
    # Get the last non assert call
    $lastCall = Get-PSCallStack | Select -Skip 1 | ?{ !$_.Command.StartsWith('Assert') } | Select -First 1
    
    "$Message. At $($lastCall.Location)"
}

function Assert-Fail {
    param(
        [parameter(Mandatory = $true)]
        [string]$Message
    )
    
    Write-Error (Get-AssertError "Failed" $Message)
}

function Assert-NotNull {
    param(
        $Value,
        [string]$Message
    )
    
    if(!$Value) {
        Write-Error (Get-AssertError "Value is null" $Message)
    }
}

function Assert-Null {
    param(
        $Value,
        [string]$Message
    )
    
    if($Value) {
        Write-Error (Get-AssertError "Value is not null" $Message)
    }
}

function Assert-AreEqual {
    param(
         [parameter(Mandatory = $true)]
         $Expected, 
         [parameter(Mandatory = $true)]
         $Actual,
         [string]$Message
    )
    
    if($Expected -ne $Actual) {
        Write-Error (Get-AssertError "Expected <$Expected> but got <$Actual>" $Message)
    } 
}

function Assert-PathExists {
    param(
          [parameter(Mandatory = $true)]
          [string]$Path, 
          [string]$Message
    )
    
    if(!(Test-Path $Path)) {
        Write-Error (Get-AssertError "The path `"$Path`" does not exist" $Message)
    }
}

function Assert-Reference {
    param(
         [parameter(Mandatory = $true)]
         $Project, 
         [parameter(Mandatory = $true)]
         [string]$Reference,
         [string]$Version
    )
    
    $assemblyReference = Get-AssemblyReference $Project $Reference
    
    Assert-NotNull $assemblyReference "Reference `"$Reference`" does not exist"
    Assert-NotNull $assemblyReference.Path "Reference `"$Reference`" exists but is broken"
    Assert-PathExists $assemblyReference.Path "Reference `"$Reference`" exists but is broken"
    
    if($Version) {
        $actualVersion = [Version]::Parse($Version)
        Assert-AreEqual $actualVersion $assemblyReference.Version
    }
}

function Assert-Build {
    param(
        [parameter(Mandatory = $true)]
        $Project,
        [string]$Configuration
    )
    
    Build-Project $Project $Configuration
    
    # Get the errors from the error list
    $errors = Get-Errors    
    
    Assert-AreEqual 0 $errors.Count "Failed to build `"$($Project.Name)`. There were errors in the list."
}