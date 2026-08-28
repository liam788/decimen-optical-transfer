Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  BUILDING OPTICAL TRANSFER - PURE NATIVE WINDOWS APP (C# / WPF)" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path $scriptDir "src"
$binDir = Join-Path $scriptDir "bin"

if (-not (Test-Path $binDir)) {
    New-Item -ItemType Directory -Path $binDir -Force | Out-Null
}

$outputExe = Join-Path $binDir "OpticalTransfer.exe"

$csFiles = Get-ChildItem -Path $srcDir -Filter "*.cs" -Recurse | Select-Object -ExpandProperty FullName
Write-Host "Found $($csFiles.Count) source files in $srcDir" -ForegroundColor Green

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
    "System.Drawing.dll",
    "System.Windows.Forms.dll"
)

$refArgs = $references | ForEach-Object { "/r:`"$_`"" }

Write-Host "Compiling with .NET Framework C# Compiler ($csc)..." -ForegroundColor Yellow
$cscArgs = @(
    "/target:winexe",
    "/optimize+",
    "/platform:anycpu",
    "/out:`"$outputExe`""
) + $refArgs + ($csFiles | ForEach-Object { "`"$_`"" })

$proc = Start-Process -FilePath $csc -ArgumentList $cscArgs -NoNewWindow -Wait -PassThru

if ($proc.ExitCode -eq 0 -and (Test-Path $outputExe)) {
    $size = (Get-Item $outputExe).Length / 1KB
    Write-Host "SUCCESS: Built $outputExe ($([math]::Round($size, 1)) KB)" -ForegroundColor Green
} else {
    Write-Host "ERROR: Build failed with exit code $($proc.ExitCode)" -ForegroundColor Red
    exit 1
}
