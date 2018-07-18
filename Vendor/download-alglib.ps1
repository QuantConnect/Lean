# properties
$installDir = "Vendor"
$archFile = "alglib-3.14.0.csharp.gpl.zip"
$url = "http://www.alglib.net/translator/re/$archFile"
$libFile = "alglibnet2.dll"

# check PS version
if ($PSVersionTable.PSVersion.Major -lt 5)
{
	Write-Error "This script requires PowerShell v 5.0 or more"
	return
}

# check current location
$PWD = (Get-Item -Path ".\")
if ($PWD.BaseName -eq $installDir){
	$installDir = $PWD.FullName
} else {
	$installDir = "$($PWD.FullName)\$installDir"
	if (!(Test-Path -Path $installDir)) {
		Write-Error "Directory 'Vendor' must exist in the solution folder"
		return
	}
}

# set locations
$archDir = "$installDir\arc"
New-Item -Path $archDir -ItemType "directory"  | Out-Null

# download archive
Invoke-WebRequest -Uri $url -OutFile "$archDir\$archFile"

# extract library
Expand-Archive "$archDir\$archFile" -DestinationPath $archDir
Move-Item -Path "$archDir\csharp\net-core\$libFile" -Destination "$installDir\$libFile"

# clean up
Remove-Item -Path $archDir -Recurse
