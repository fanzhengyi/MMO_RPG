$exe = 'C:\Program Files\dotnet\dotnet.exe'
$dll = 'D:\My\MyDemo\MMO_RPG\MyDemo\Server\Main\bin\Debug\net8.0\Main.dll'
$wd  = 'D:\My\MyDemo\MMO_RPG\MyDemo\Server\Main\bin\Debug\net8.0'
$out = 'D:\My\MyDemo\MMO_RPG\MyDemo\Server\Main\stdout.log'
$err = 'D:\My\MyDemo\MMO_RPG\MyDemo\Server\Main\stderr.log'

# kill any existing Main
Get-Process Main -ErrorAction SilentlyContinue | Stop-Process -Force

$p = Start-Process -FilePath $exe -ArgumentList @($dll,'-m','Develop') `
        -WorkingDirectory $wd -RedirectStandardOutput $out -RedirectStandardError $err `
        -PassThru -WindowStyle Hidden
Write-Output ('PID:' + $p.Id)

Start-Sleep -Seconds 45

$alive = Get-Process -Id $p.Id -ErrorAction SilentlyContinue
if ($alive) {
    Write-Output 'ALIVE: yes (killing now)'
    $alive | Stop-Process -Force
} else {
    Write-Output 'ALIVE: no (exited)'
}

Write-Output '---STDOUT---'
Get-Content $out
Write-Output '---STDERR---'
Get-Content $err