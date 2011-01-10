
# Tests for Install-Package intellisense

function Test-TabExpansionForInstallPackageShowSuggestionsForPackageId {
    # Act
    $suggestions = TabExpansion 'Install-Package ' ''

    # Assert
    Assert-True $suggestions.Count -gt 30
}

function Test-TabExpansionForInstallPackageShowSuggestionsForPackageIdWithFilter {

    # Act
    $suggestions = TabExpansion 'Install-Package sql' 'sql'

    # Assert
    Assert-True $suggestions.Count -gt 0
    $suggestions | ForEach-Object { Assert-True $_.StartsWith('sql', 'OrdinalIgnoreCase') }
}

function Test-TabExpansionForInstallPackageSupportsVersion {
    # Act
    $suggestions = TabExpansion 'Install-Package Antlr -Version ' ''

    # Assert
    Assert-AreEqual 2 $suggestions.Count
    Assert-AreEqual '3.1.1' $suggestions[0]
    Assert-AreEqual '3.1.3.42154' $suggestions[1]
}

function Test-TabExpansionForInstallPackageShowSuggestionsForProjectName {

    # Arrange
    $projectNames = @()
    
    for ($i = 0; $i -lt 3; $i++) {
        $project = New-ConsoleApplication 
        $projectNames = $projectNames + $project.Name
    }
    
    $sortedProjectNames = $projectNames | Sort-Object

    # Act
    $suggestions = TabExpansion 'Install-Package sqlce ' ''

    # Assert
    Assert-AreEqual 3 $suggestions.count
    for ($i = 0; $i -lt 3; $i++) {
        Assert-AreEqual $sortedProjectNames[$i] $suggestions[$i]
    }
}

# Tests for Uninstall-Package intellisense

function Test-TabExpansionForUninstallPackageShowSuggestionsForPackageId {
    # Arrange
    $project = New-ConsoleApplication
    Install-Package AntiXSS -Project $project.Name
    Install-Package elmah -Project $project.Name

    # Act
    $suggestions = TabExpansion 'Uninstall-Package ' ''

    # Assert
    Assert-AreEqual 2 $suggestions.count
    Assert-AreEqual 'AntiXSS' $suggestions[0]
    Assert-AreEqual 'elmah' $suggestions[1]
}

function Test-TabExpansionForUninstallPackageShowSuggestionsForProjectNames {
    # Arrange
    $projectNames = @()
    
    for ($i = 0; $i -lt 3; $i++) {
        $project = New-ConsoleApplication 
        $projectNames = $projectNames + $project.Name
    }
    
    $sortedProjectNames = $projectNames | Sort-Object

    # Act
    $suggestions = TabExpansion 'Uninstall-Package sqlce ' ''

    # Assert
    Assert-AreEqual 3 $suggestions.count
    for ($i = 0; $i -lt 3; $i++) {
        Assert-AreEqual $sortedProjectNames[$i] $suggestions[$i]
    }
}

# Tests for Update-Package intellisense

function Test-TabExpansionForUpdatePackageShowSuggestionsForPackageId {
    # Arrange
    $project = New-ConsoleApplication
    Install-Package 'NHibernate' -Project $project.Name -Version '2.1.2.4000'

    # Act
    $suggestions = TabExpansion 'Update-Package ' ''
    $nhibernateUpdates = $suggestions | Where-Object { 'NHibernate' -eq $_ }

    # Assert
    Assert-True $nhibernateUpdates.count -ge 1
}

function Test-TabExpansionForUpdatePackageShowSuggestionsForProjectNames {
    # Arrange
    $projectNames = @()
    
    for ($i = 0; $i -lt 3; $i++) {
        $project = New-ConsoleApplication 
        $projectNames = $projectNames + $project.Name
    }
    
    $sortedProjectNames = $projectNames | Sort-Object

    # Act
    $suggestions = TabExpansion 'Update-Package sqlce ' ''

    # Assert
    Assert-AreEqual 3 $suggestions.count
    for ($i = 0; $i -lt 3; $i++) {
        Assert-AreEqual $sortedProjectNames[$i] $suggestions[$i]
    }
}

# Tests to make sure private functions & cmdlets do not show up in the intellisense

function Test-TabExpansionDoNotSuggestFindPackage() {
    
    # Act
    $suggestions = TabExpansion 'Find-Pac' 'Find-Pac'

    # Assert
    Assert-Null $suggestions
}

function Test-TabExpansionDoNotSuggestGetProjectName() {
    
    # Act
    $suggestions = TabExpansion 'GetProjectN' 'GetProjectN'

    # Assert
    Assert-Null $suggestions
}

function Test-CustomTabExpansion {
    # Arrange
    function global:Foo($Name) {
        "Hello $Name"
    }

    Register-TabExpansion Foo @{ 'Name' = { 'David Fowler', 'John Doe', "John's Hide Out", "Woah's", "A`tB", "G" } }

    # Act
    $suggestions = TabExpansion 'Foo ' ''

    # Assert
    Assert-NotNull $suggestions
    Assert-AreEqual 6 $suggestions.Count
    Assert-AreEqual "'A`tB'" $suggestions[0]
    Assert-AreEqual "'David Fowler'" $suggestions[1]
    Assert-AreEqual "G" $suggestions[2]
    Assert-AreEqual "'John Doe'" $suggestions[3]
    Assert-AreEqual "'John''s Hide Out'" $suggestions[4]
    Assert-AreEqual "'Woah''s'" $suggestions[5]

    # Remove the function from global scope
    rm function:\Foo
}


function Test-ComplexCustomTabExpansion {
    # Arrange
    function global:Foo($Name, $Age) {
    }

    $ages = @{
        'David''s Sister''s Brother''s Age' = '10'
        'Phil' = '11'
        'David''s Dog' = '12'
        'John Doe' = '14'
    }

    Register-TabExpansion Foo @{ 
        'Name' = { $ages.Keys }
        'Age' = { param($context) $ages[$context.Name] }
    }

    # Act
    $philAge = TabExpansion "Foo -Name Phil -Age " ""
    $johnDoeAge = TabExpansion "Foo -Name 'John Doe' -Age " ""
    $dogAge = TabExpansion "Foo -Name 'David''s Dog' -Age " ""
    $davidAge = TabExpansion "Foo 'David''s Sister''s Brother''s Age' -Age " ""

    Assert-AreEqual 11 $philAge
    Assert-AreEqual 14 $johnDoeAge
    Assert-AreEqual 12 $dogAge
    Assert-AreEqual 10 $davidAge

    # Remove the function from global scope
    rm function:\Foo
}
