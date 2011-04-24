
function Test-GetHelpReturnExactlyOneTopic {
	# Act
	$p = @(Get-Help NuGet)

	# Assert
	Assert-AreEqual 1 $p.Length
}