$ErrorActionPreference = 'Stop'
$testRoot = $PSScriptRoot
$repoRoot = Split-Path -Parent $testRoot
$compiler = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'Visual Studio 2022 Build Tools with the C# compiler is required.' }
$outputPath = Join-Path $testRoot 'bin'
$null = New-Item -ItemType Directory -Path $outputPath -Force
foreach ($architecture in @('x64', 'x86')) {
    & $compiler /nologo /target:exe "/platform:$architecture" "/out:$outputPath\CaptureSmokeTests.$architecture.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll (Join-Path $repoRoot 'WindowSharingShower\WindowHandler.cs') (Join-Path $testRoot 'CaptureSmokeTests.cs')
    if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $architecture" }
}
& (Join-Path $outputPath 'CaptureSmokeTests.x64.exe')
if ($LASTEXITCODE -ne 0) { throw 'Capture smoke tests failed.' }
