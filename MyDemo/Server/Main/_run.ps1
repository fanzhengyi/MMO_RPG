$ErrorActionPreference = 'Stop'

$serverRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $serverRoot 'Server.sln'
$outputPath = Join-Path $PSScriptRoot 'bin\Debug\net8.0'

dotnet build $solutionPath --configuration Debug
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Push-Location $outputPath
try {
    dotnet .\Main.dll --m Develop
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
