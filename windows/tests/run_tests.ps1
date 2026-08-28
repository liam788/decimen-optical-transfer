$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path (Split-Path -Parent $scriptDir) "src"
$testExe = Join-Path $scriptDir "TestFountainCodec.exe"

$csFiles = @(
    (Join-Path $srcDir "Core\FountainMath.cs"),
    (Join-Path $srcDir "Core\ProtocolDcf2.cs"),
    (Join-Path $srcDir "Core\FountainCodec.cs"),
    (Join-Path $srcDir "Core\QrEngine.cs"),
    (Join-Path $srcDir "HAL\ICameraProvider.cs"),
    (Join-Path $srcDir "HAL\WindowsCameraProvider.cs"),
    (Join-Path $srcDir "State\SessionModels.cs"),
    (Join-Path $srcDir "State\TxSessionController.cs"),
    (Join-Path $srcDir "State\RxSessionController.cs"),
    (Join-Path $scriptDir "TestFountainCodec.cs")
)

$netDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$wpfDir = Join-Path $netDir "WPF"
$csc = Join-Path $netDir "csc.exe"

$references = @(
    (Join-Path $wpfDir "PresentationFramework.dll"),
    (Join-Path $wpfDir "PresentationCore.dll"),
    (Join-Path $wpfDir "WindowsBase.dll"),
    (Join-Path $netDir "System.Xaml.dll"),
    "System.dll",
    "System.Core.dll",
    "System.Drawing.dll"
)

$refArgs = $references | ForEach-Object { "/r:`"$_`"" }

$cscArgs = @(
    "/target:exe",
    "/optimize+",
    "/out:`"$testExe`""
) + $refArgs + ($csFiles | ForEach-Object { "`"$_`"" })

$proc = Start-Process -FilePath $csc -ArgumentList $cscArgs -NoNewWindow -Wait -PassThru

if ($proc.ExitCode -eq 0 -and (Test-Path $testExe)) {
    Write-Host "Test harness compiled successfully. Running test suite..." -ForegroundColor Green
    & $testExe
    $testResult = $LASTEXITCODE
    Remove-Item $testExe -ErrorAction SilentlyContinue
    exit $testResult
} else {
    Write-Host "Test compilation failed!" -ForegroundColor Red
    exit 1
}
