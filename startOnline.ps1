& dotnet build
if (-not $?) {
	throw 'Build failed'
}

[xml]$config = Get-Content "$PSScriptRoot\Config.props"
$roundsDir = $config.Project.PropertyGroup.RoundsDir
$bepinexDir = $config.Project.PropertyGroup.BepInExDir

Import-Module .\tools\WindowUtils.psm1

function Start-Game([System.Int32]$Monitor, [System.String]$Arguments) {
	$process = Start-Process "$roundsDir\Rounds.exe" -PassThru -ArgumentList "$Arguments -- --doorstop-enable true --doorstop-target-assembly ""$bepinexDir\core\BepInEx.Preloader.dll"""
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
	$process1 = Start-Game -Monitor 1 -Arguments "-autoHost eu:1234"
	Start-Sleep -Seconds 2
	$process2 = Start-Game -Monitor 0 -Arguments "-autoConnect eu:1234"

	while ($process1.HasExited -eq $false -and $process2.HasExited -eq $false) {
		Start-Sleep -Milliseconds 100
	}
}
finally {
	$process1, $process2 | ForEach-Object {
		if (-not ($_ -eq $null) -and -not $_.HasExited) {
			Stop-Process -Id $_.Id
		}
	}

	Pop-Location
}