param()
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$serverProject = Join-Path $repoRoot 'Server/Main/Main.csproj'
$integrationProject = Join-Path $PSScriptRoot 'Integration.csproj'
$serverOutput = Join-Path $repoRoot 'Server/Main/bin/Debug/net8.0'
$testDll = Join-Path $PSScriptRoot 'bin/Debug/net8.0/Integration.dll'

# 使用独立数据库和临时运行目录；不修改开发服务器配置及已有账号。
foreach ($port in @(20001, 20002, 20003, 20050, 20051)) {
    if (Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue) {
        throw "Local test port $port is in use. Stop the development server before this isolated test."
    }
}
dotnet build $serverProject
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build $integrationProject
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$testId = [Guid]::NewGuid().ToString('N')
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ('mmo-sync-review-' + $testId)
$databaseName = 'fantasy_sync_review_' + $testId
$serverProcess = $null
$testExitCode = 1
try {
    New-Item -ItemType Directory -Path $testDirectory | Out-Null
    Copy-Item -Path (Join-Path $serverOutput '*') -Destination $testDirectory -Recurse
    $configurationPath = Join-Path $testDirectory 'Fantasy.config'
    [xml]$configuration = Get-Content -LiteralPath $configurationPath -Raw
    $configuration.fantasy.server.worlds.world.database.dbName = $databaseName
    $configuration.Save($configurationPath)
    $serverProcess = Start-Process -FilePath 'dotnet' -ArgumentList @('Main.dll', '--m', 'Develop') `
        -WorkingDirectory $testDirectory -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $testDirectory 'server.log') `
        -RedirectStandardError (Join-Path $testDirectory 'server.err.log')
    $ready = $false
    for ($attempt = 0; $attempt -lt 100; $attempt++) {
        if ($serverProcess.HasExited) { throw "Test server failed. See $testDirectory/server.log" }
        if ((Get-Content -LiteralPath (Join-Path $testDirectory 'server.log') -Raw) -match 'Process:3 Startup Complete') {
            $ready = $true
            break
        }
        Start-Sleep -Milliseconds 100
    }
    if (!$ready) { throw "Test server startup timed out. See $testDirectory/server.log" }
    dotnet $testDll
    $testExitCode = $LASTEXITCODE
}
finally {
    if ($null -ne $serverProcess -and !$serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id
        $serverProcess.WaitForExit()
    }
    dotnet $testDll --cleanup-db $databaseName
    if ($LASTEXITCODE -ne 0) { $testExitCode = 1 }
    # 日志保留在临时目录，失败后可以复查；目录中没有项目源代码。
    Write-Output "Integration logs: $testDirectory"
}
exit $testExitCode
