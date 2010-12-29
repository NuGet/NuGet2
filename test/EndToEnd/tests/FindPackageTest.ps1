function Test-FindPackageReturnsExpectedNumberOfPackages {

    # Act
    $packages = Find-Package -Source 'https://go.microsoft.com/fwlink/?LinkID=206669'
    
    # Assert
    Assert-AreEqual 30 $packages.Count 'Find-Package cmdlet returns the wrong number of packages.'
}