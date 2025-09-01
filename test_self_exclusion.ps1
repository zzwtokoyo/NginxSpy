# 测试NginxSpy是否会将自己排除在nginx进程检测之外

Write-Host "当前运行的NginxSpy进程:"
Get-Process | Where-Object {$_.ProcessName -eq 'NginxSpy'} | Format-Table ProcessName, Id, StartTime

Write-Host "`n等待5秒让应用程序完全启动..."
Start-Sleep -Seconds 5

Write-Host "`n检查日志文件中的nginx进程检测信息:"
$logFile = "./logs/nginxspy-$(Get-Date -Format 'yyyyMMdd').txt"
if (Test-Path $logFile) {
    Write-Host "查看最新的日志条目:"
    Get-Content $logFile | Select-Object -Last 20
} else {
    Write-Host "日志文件不存在: $logFile"
}

Write-Host "`n检查是否有nginx相关进程:"
Get-Process | Where-Object {$_.ProcessName -like '*nginx*'} | Format-Table ProcessName, Id, StartTime