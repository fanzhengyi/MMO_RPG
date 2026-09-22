$ips = @('192.168.150.41','127.0.0.1','0.0.0.0')
$port = 11001
foreach ($ip in $ips) {
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Parse($ip), $port)
        $listener.Start()
        Write-Output ("OK bind {0}:{1}" -f $ip, $port)
        $listener.Stop()
    } catch {
        Write-Output ("FAIL {0}:{2} -> {1}" -f $ip, $_.Exception.Message, $port)
    }
}