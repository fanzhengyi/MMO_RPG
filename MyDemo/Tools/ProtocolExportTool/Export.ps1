param([string]$ConfigPath = "$PSScriptRoot/ExporterSettings.json")

$ErrorActionPreference = 'Stop'
$configPath = [System.IO.Path]::GetFullPath($ConfigPath)
$settings = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
$clientDirectory = [System.IO.Path]::GetFullPath(
    [System.IO.Path]::Combine($PSScriptRoot, $settings.Export.NetworkProtocolClientDirectory.Value))
$helperPath = Join-Path $clientDirectory 'NetworkProtocolHelper.cs'

# Keep the project-maintained client helper after protocol generation.
$helperSource = [System.IO.File]::ReadAllBytes($helperPath)
Push-Location -LiteralPath $PSScriptRoot
try {
    & dotnet (Join-Path $PSScriptRoot 'Fantasy.ProtocolExportTool.dll') export --silent --config $configPath
    if ($LASTEXITCODE -ne 0) {
        throw "Protocol export failed with exit code $LASTEXITCODE."
    }
    $serverDirectory = [System.IO.Path]::GetFullPath(
        [System.IO.Path]::Combine($PSScriptRoot, $settings.Export.NetworkProtocolServerDirectory.Value))
    foreach ($directory in @($clientDirectory, $serverDirectory)) {
        foreach ($file in Get-ChildItem -LiteralPath $directory -Filter '*.cs' -File) {
            $source = [System.IO.File]::ReadAllText($file.FullName)
            $formatted = [System.Text.RegularExpressions.Regex]::Replace($source, '(?m)[\t ]+(?=\r?$)', '')
            if ($formatted -ne $source) {
                [System.IO.File]::WriteAllText($file.FullName, $formatted, [System.Text.UTF8Encoding]::new($false))
            }
        }
    }
}
finally {
    try { [System.IO.File]::WriteAllBytes($helperPath, $helperSource) }
    finally { Pop-Location }
}
