param([int]$PortOffset = 5000)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$serverProject = Join-Path $repoRoot 'Server/Main/Main.csproj'
$integrationProject = Join-Path $PSScriptRoot 'Integration.csproj'
$testDll = Join-Path $PSScriptRoot 'bin/Debug/net8.0/Integration.dll'

# 使用独立数据库和临时运行目录；不修改开发服务器配置及已有账号。
foreach ($port in @(20001, 20002, 20003, 20050, 20051, 11001, 11002, 11003, 11050, 11051, 21001)) {
    $testPort = $port + $PortOffset
    if (Get-NetTCPConnection -State Listen -LocalPort $testPort -ErrorAction SilentlyContinue) {
        throw "Isolated test port $testPort is in use; choose another -PortOffset."
    }
}
dotnet build (Join-Path $repoRoot 'Server/Hotfix/Hotfix.csproj') --no-restore -m:1 -nr:false -p:BuildInParallel=false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build $integrationProject --no-restore -m:1 -nr:false -p:BuildInParallel=false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$testId = [Guid]::NewGuid().ToString('N')
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ('mmo-sync-review-' + $testId)
$databaseName = 'fantasy_sync_review_' + $testId
$serverProcess = $null
$testExitCode = 1
$previousOffset = $env:MMO_TEST_PORT_OFFSET
try {
    New-Item -ItemType Directory -Path $testDirectory | Out-Null
    # 单独输出 Main，避免覆盖正在运行的开发服务器程序集。
    dotnet build $serverProject --no-restore -m:1 -nr:false -p:BuildInParallel=false -p:BuildProjectReferences=false -o $testDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Isolated server build failed.' }
    Copy-Item -LiteralPath (Join-Path $repoRoot 'Server/Entity/bin/Debug/net8.0/Entity.dll') -Destination $testDirectory
    $configurationPath = Join-Path $testDirectory 'Fantasy.config'
    [xml]$configuration = Get-Content -LiteralPath $configurationPath -Raw
    $configuration.fantasy.server.worlds.world.database.dbName = $databaseName
    foreach ($scene in $configuration.fantasy.server.scenes.scene) {
        $scene.innerPort = [string]([int]$scene.innerPort + $PortOffset)
        if ([int]$scene.outerPort -gt 0) { $scene.outerPort = [string]([int]$scene.outerPort + $PortOffset) }
    }
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
    $env:MMO_TEST_PORT_OFFSET = [string]$PortOffset
    dotnet $testDll
    $testExitCode = $LASTEXITCODE
}
finally {
    $env:MMO_TEST_PORT_OFFSET = $previousOffset
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
