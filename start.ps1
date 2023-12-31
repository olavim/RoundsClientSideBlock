& dotnet build
if (-not $?) {
	throw 'Build failed'
}

[xml]$config = Get-Content "$PSScriptRoot\Config.props"
$roundsDir = $config.Project.PropertyGroup.RoundsDir
$bepinexDir = $config.Project.PropertyGroup.BepInExDir

Import-Module .\tools\WindowUtils.psm1

function Start-Game([System.Int32]$Monitor) {
	$process = Start-Process "$roundsDir\Rounds.exe" -PassThru -ArgumentList "-- --doorstop-enable true --doorstop-target-assembly ""$bepinexDir\core\BepInEx.Preloader.dll"""
	Start-Sleep -Seconds 1

	$consoleWindow = $process.MainWindowHandle
	Move-Window -WindowHandle $consoleWindow -Monitor $Monitor -X 30 -Y 30
	Show-Window -WindowHandle $consoleWindow
	Start-Sleep -Seconds 1

	$appWindow = Get-ChildWindow -Process $process
	Move-Window -WindowHandle $appWindow -Monitor $Monitor -X 30 -Y 60 -FromRight -FromBottom
	Show-Window -WindowHandle $appWindow

	return $process
}

try {
	$process = Start-Game -Monitor 0

	while ($process.HasExited -eq $false) {
		Start-Sleep -Milliseconds 100
	}
}
finally {
	if (-not ($process -eq $null) -and -not $process.HasExited) {
		Stop-Process -Id $process.Id
	}

	Pop-Location
}
