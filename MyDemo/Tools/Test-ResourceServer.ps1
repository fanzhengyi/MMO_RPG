param(
    [string]$BaseUrl = 'http://127.0.0.1/CDN/StandaloneWindows64/DefaultPackage',
    [string]$ReleaseDirectory = (Join-Path $PSScriptRoot '../ResourceServer/CDN/StandaloneWindows64/DefaultPackage'),
    [switch]$AllBundles
)

$ErrorActionPreference = 'Stop'
$BaseUrl = $BaseUrl.TrimEnd('/')
$versionName = 'DefaultPackage.version'
$version = (Get-Content -LiteralPath (Join-Path $ReleaseDirectory $versionName) -Raw).Trim()
$names = @($versionName, "DefaultPackage_$version.hash", "DefaultPackage_$version.bytes")
$bundles = @(Get-ChildItem -LiteralPath $ReleaseDirectory -File | Where-Object { $_.Name -notin $names -and $_.Name -notmatch '^DefaultPackage_.+\.(bytes|hash)$' -and $_.Extension -notin '.report','.json','.xml','.pending' } | Sort-Object Length)
if ($bundles.Count -eq 0) { throw '发布目录没有真实 Bundle；请先在 Unity 执行 Tools/Resources/Build and Stage Windows Bundles。' }
$names += if ($AllBundles) { $bundles.Name } else { $bundles[0].Name }
$verified = foreach ($name in $names) {
    $temporaryFile = [System.IO.Path]::GetTempFileName()
    try {
        $uri = "$BaseUrl/$([System.Uri]::EscapeDataString($name))"
        Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 60 -OutFile $temporaryFile
        $expected = (Get-FileHash -LiteralPath (Join-Path $ReleaseDirectory $name) -Algorithm SHA256).Hash
        $actual = (Get-FileHash -LiteralPath $temporaryFile -Algorithm SHA256).Hash
        if ($actual -ne $expected) { throw "远程文件与发布目录不一致：$uri" }
        [pscustomobject]@{ File = $name; Bytes = (Get-Item -LiteralPath $temporaryFile).Length; SHA256 = $actual }
    }
    finally {
        Remove-Item -LiteralPath $temporaryFile -Force
    }
}
[pscustomobject]@{ BaseUrl = $BaseUrl; Version = $version; VerifiedCount = @($verified).Count; Files = @($verified) }
