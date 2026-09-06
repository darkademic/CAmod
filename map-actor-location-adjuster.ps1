[CmdletBinding()]
param(
	[Parameter(Mandatory = $true, Position = 0)]
	[ValidateNotNullOrEmpty()]
	[string]$MapPath,

	[Parameter(Mandatory = $true, Position = 1)]
	[int]$X,

	[Parameter(Mandatory = $true, Position = 2)]
	[int]$Y
)

$resolvedMapPath = Resolve-Path -LiteralPath $MapPath -ErrorAction Stop
$locationPattern = '^(?<prefix>\s*Location:\s*)(?<x>-?\d+)\s*,\s*(?<y>-?\d+)(?<suffix>\s*(?:#.*)?)$'
$updatedLocations = 0

$updatedLines = foreach ($line in [System.IO.File]::ReadLines($resolvedMapPath))
{
	$match = [regex]::Match($line, $locationPattern)
	if ($match.Success)
	{
		$updatedLocations++
		'{0}{1},{2}{3}' -f $match.Groups['prefix'].Value, ([int]$match.Groups['x'].Value + $X), ([int]$match.Groups['y'].Value + $Y), $match.Groups['suffix'].Value
	}
	else
	{
		$line
	}
}

[System.IO.File]::WriteAllLines($resolvedMapPath, $updatedLines)
Write-Host "Adjusted $updatedLocations Location entries in '$resolvedMapPath'."