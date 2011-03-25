

function Test-CurrentLocationSetToUserProfileWhenThereIsNoSolution {
    
    # Act
    Close-Solution

    # Assert
    # Assert-AreEqual $env:UserProfile $pwd
}

function Test-CurrentLocationSetToSolutionDirWhenSolutionIsOpen {
    
    # Act
    Ensure-Solution
    $solutionDir = Get-SolutionDir

    # Assert
    Assert-AreEqual $solutionDir $pwd
}

function Test-CurrentLocationSetBackToUserProfileWhenSolutionIsOpenAndThenClosed {
    
    # Arrange
    Ensure-Solution
    
    # Act
    Close-Solution

    # Assert
    Assert-AreEqual $env:UserProfile $pwd
}