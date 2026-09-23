Write-Output '=== UDP ports 2000x ==='
netstat -ano -p udp | Select-String ':2000'
Write-Output '=== Server processes ==='
Get-Process | Where-Object { $_.ProcessName -match 'Server|Fantasy|dotnet' } | Format-Table Id, ProcessName -AutoSize
Write-Output '=== TCP ports 2000x/1100x ==='
netstat -ano -p tcp | Select-String ':2000|:1100'
