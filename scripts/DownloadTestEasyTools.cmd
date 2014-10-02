param([string]$destinationFolder="")

function Extract-Zip
{
	param([string]$zipfilename, [string]$destination)

	if(test-path($zipfilename))
	{
		$shellApplication = new-object -com shell.application
		$zipPackage = $shellApplication.NameSpace($zipfilename)
		$destinationFolder = $shellApplication.NameSpace($destination)
		$destinationFolder.CopyHere($zipPackage.Items())
	}
}

$currentDirectory = Split-Path $MyInvocation.MyCommand.Definition -Parent -Resolve
if ($destinationFolder -eq "")
{
	$destinationFolder=$currentDirectory
}

$IEDriverZip="IEDriverServer_Win32_2.39.0.zip"
$IEDriverZipPath=$destinationFolder + $IEDriverZip
$IEDriverUrl="https://selenium.googlecode.com/files/" + $IEDriverZip
Invoke-WebRequest $IEDriverUrl -OutFile $IEDriverZipPath

Extract-Zip -zipfilename $IEDriverZipPath -destination $destinationFolder