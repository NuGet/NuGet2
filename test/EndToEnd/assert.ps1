# Assert functions
function Assert-Fail {
    param(
        [parameter(mandatory = $true)]
        $Message
    )
    
    Get-PSCallStack
    
    # Get the cal up teh stack before this one
    $stack = @(Get-PSCallStack)[1]
    
    $lastCall = Get-PSCallStack | ?{ !$_.Command.StartsWith('Assert') } | Select -First 1
    
    Write-Error "$($stack.Command) failed $Message at $($lastCall.Location)"
}

function Assert-NotNull {
    param(
        [parameter(mandatory = $true)]
        $Value,
        $Message
    )
    if(!$Message) {
        $Message = "Value is null"
    }
    
    if(!$Value) {
        Assert-Fail $Message
    }
}

function Assert-Null {
    param(
        $Value,
        $Message
    )
    if(!$Message) {
        $Message = "Value is not null"
    }
    
    if($Value) {
        Assert-Fail $Message
    }
}

function Assert-AreEqual {
    param(
         [parameter(mandatory = $true)]
         $Expected, 
         [parameter(mandatory = $true)]
         $Actual
    )
    
    if($Expected -ne $Actual) {
        Assert-Fail "Expected $Expected but got $Actual"
    } 
}

function Assert-PathExists {
    param(
          [parameter(mandatory = $true)]
          [string]$Path, 
          [string]$Message
    )
    
    if(!(Test-Path $Path)) {
        if(!$Message) {
            $Message = "Path `"$Path`" does not exist"
        }        
        Assert-Fail $Message
    }
}

function Assert-Reference {
    param(
         [parameter(mandatory = $true)]
         $Project, 
         [parameter(mandatory = $true)]
         [string]$Reference,
         [string]$Version
    )
    
    $assemblyReference = Get-AssemblyReference $Project $Reference
    
    if(!$assemblyReference) {
        Assert-Fail "Reference `"$Reference`" does not exist"
    }
    elseif(!$assemblyReference.Path) {
        Assert-Fail "Reference `"$Reference`" exists but is broken"
    }
    
    if($Version) {
        Assert-AreEqual $Version $assemblyReference.Version
    }
}

function Assert-Build {
    param(
        [parameter(Mandatory = $true)]
        $Project,
        [string]$Configuration
    )
    
    Build-Project $Project $Configuration
    
    # Make sure there are no errors in the error list
    $errorList = $dte.Windows | ?{ $_.Caption -eq 'Error List' } | Select -First 1
    
    if(!$errorList) {
        Assert-Fail "Unable to locate the error list"
    }
    
    $errors = $errorList.Object.ErrorItems
    
    if($errors.Count -gt 0) {
        if($errors.Count -eq 1) {
            $errorsMessage = "There was 1 error"
        }   
        else {
            $errorsMessage = "There were $($errors.Count) erros"
        }
        # REVIEW: Should we show the error window?
        Assert-Fail "Failed to build `"$($Project.Name)`". $errorsMessage"
    }
}