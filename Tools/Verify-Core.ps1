param([string]$CompilerPath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationRoot = Join-Path $projectRoot 'work\verification'
New-Item -ItemType Directory -Path $verificationRoot -Force | Out-Null
if (-not $CompilerPath) {
    $CompilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $CompilerPath)) {
    throw 'A .NET Framework C# compiler is required for this standalone Windows check. Unity Test Runner is the cross-platform alternative.'
}
$moduleOrder = @(
    @{ Name='Synaps.Core'; Folder='Core'; References=@() },
    @{ Name='Synaps.Search'; Folder='Search'; References=@('Synaps.Core') },
    @{ Name='Synaps.AI'; Folder='AI'; References=@('Synaps.Core') },
    @{ Name='Synaps.Input'; Folder='Input\Contracts'; References=@() },
    @{ Name='Synaps.KnowledgeGraph'; Folder='KnowledgeGraph'; References=@('Synaps.Core') },
    @{ Name='Synaps.Data'; Folder='Data'; References=@('Synaps.Core','Synaps.KnowledgeGraph') }
)
$dllPaths = @()
foreach ($module in $moduleOrder) {
    $sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot ('Assets\Synaps\' + $module.Folder)) -Filter '*.cs' | ForEach-Object FullName)
    $dllPath = Join-Path $verificationRoot ($module.Name + '.dll')
    $arguments = @('/nologo','/target:library','/warnaserror','/codepage:65001',('/out:' + $dllPath))
    foreach ($reference in $module.References) { $arguments += '/reference:' + (Join-Path $verificationRoot ($reference + '.dll')) }
    $arguments += $sourceFiles
    & $CompilerPath @arguments
    if ($LASTEXITCODE -ne 0) { throw ('Compilation failed: ' + $module.Name) }
    $dllPaths += $dllPath
}
$runner = Join-Path $verificationRoot 'SynapsChecks.exe'
$arguments = @('/nologo','/target:exe','/warnaserror','/codepage:65001',('/out:' + $runner))
foreach ($dll in $dllPaths) { $arguments += '/reference:' + $dll }
$arguments += Join-Path $PSScriptRoot 'Standalone\NUnitShim.cs'
$arguments += Join-Path $PSScriptRoot 'Standalone\Runner.cs'
$arguments += Join-Path $projectRoot 'Assets\Synaps\Tests\EditMode\CoreBehaviorTests.cs'
& $CompilerPath @arguments
if ($LASTEXITCODE -ne 0) { throw 'Behavior check compilation failed.' }
& $runner
if ($LASTEXITCODE -ne 0) { throw 'Behavior checks failed.' }
Write-Output 'Six domain assemblies compiled without Unity references. Unity JSON and scene checks require Unity Editor.'
