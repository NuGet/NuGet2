

function Test-OpenProjectPageOpenProjectUrlByDefault {
    param(
        $context
    )

    # Act
    $p = Open-PackagePage 'OpenPackagePageTestPackage' -Source $context.RepositoryRoot -WhatIf -PassThru

    # Assert
    Assert-AreEqual 'http://codeplex.com' $p.OriginalString
}

function Test-OpenProjectPageOpenLicenseUrlIfLicenseParameterIsSet {
    param(
        $context
    )

    # Act
    $p = Open-PackagePage 'OpenPackagePageTestPackage' -Source $context.RepositoryRoot -License -WhatIf -PassThru

    # Assert
    Assert-AreEqual 'http://bing.com' $p.OriginalString
}

function Test-OpenProjectPageOpenReportAbuseUrlIfReportAbuseParameterIsSet {
    # Act
    $p = Open-PackagePage elmah -Report -WhatIf -PassThru

    # Assert
    Assert-AreEqual 'http://nuget.org/Package/ReportAbuse/elmah/1.1' $p.OriginalString
}

function Test-OpenProjectPageFailsIfIdIsSetToTheWrongValue {
    param(
        $context
    )

    # Act & Assert

    Assert-Throws { 
        Open-PackagePage 'OpenPackagePageTestPackage_Wrong' -Source $context.RepositoryRoot
    } "Package with the Id 'OpenPackagePageTestPackage_Wrong' is not found in the specified source."
}

function Test-OpenProjectPageFailsIfVersionIsSetToTheWrongValue {
    param(
        $context
    )

    # Act & Assert

    Assert-Throws { 
        Open-PackagePage 'OpenPackagePageTestPackage' -Version 4.2 -Source $context.RepositoryRoot
    } "Package with the Id 'OpenPackagePageTestPackage' and version '4.2' is not found in the specified source."
}

function Test-OpenProjectPageFailsIfReportUrlIsNotAvailable {
    param(
        $context
    )

    # Act & Assert

    Assert-Throws { 
        Open-PackagePage 'OpenPackagePageTestPackage' -Report -Source $context.RepositoryRoot
    } "The package 'OpenPackagePageTestPackage 1.0' does not provide the requested URL."
}

function Test-OpenProjectPageFailsIfProjectUrlIsNotAvailable {
    param(
        $context
    )

    # Act & Assert

    Assert-Throws { 
        Open-PackagePage 'PackageWithGacReferences' -Source $context.RepositoryRoot
    } "The package 'PackageWithGacReferences 1.0' does not provide the requested URL."
}

function Test-OpenProjectPageFailsIfLicenseUrlIsNotAvailable {
    param(
        $context
    )

    # Act & Assert

    Assert-Throws { 
        Open-PackagePage 'PackageWithGacReferences' -License -Source $context.RepositoryRoot
    } "The package 'PackageWithGacReferences 1.0' does not provide the requested URL."
}