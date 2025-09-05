# 测试单次点击停止功能脚本
# Test Single Click Stop Functionality

Write-Host "=== Testing Single Click Stop Fix ===" -ForegroundColor Green
Write-Host ""

# 检查是否有运行中的nginx进程
$nginxProcesses = Get-Process | Where-Object { $_.ProcessName -like "*nginx*" -and $_.ProcessName -ne "NginxSpy" }

if ($nginxProcesses.Count -eq 0) {
    Write-Host "No nginx processes found. Please start nginx first." -ForegroundColor Yellow
    Write-Host "You can start nginx manually or use NginxSpy to start it." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit
}

Write-Host "Found nginx processes:" -ForegroundColor Cyan
foreach ($proc in $nginxProcesses) {
    Write-Host "  - PID: $($proc.Id), Name: $($proc.ProcessName)" -ForegroundColor White
}
Write-Host ""

# 启动NginxSpy应用程序
Write-Host "Starting NginxSpy application..." -ForegroundColor Green
Write-Host ""
Write-Host "Testing Instructions:" -ForegroundColor Yellow
Write-Host "1. Wait for NginxSpy to load completely" -ForegroundColor White
Write-Host "2. Go to the Process Management tab" -ForegroundColor White
Write-Host "3. Select a running nginx instance" -ForegroundColor White
Write-Host "4. Click the Stop button ONCE" -ForegroundColor White
Write-Host "5. Verify that the process stops immediately without requiring a second click" -ForegroundColor White
Write-Host ""
Write-Host "Expected behavior:" -ForegroundColor Cyan
Write-Host "- Process should stop with a single click" -ForegroundColor Green
Write-Host "- No need to click stop button twice" -ForegroundColor Green
Write-Host "- Status should update to 'Stopped' immediately" -ForegroundColor Green
Write-Host ""

try {
    # 启动NginxSpy
    $nginxSpyPath = Join-Path $PSScriptRoot "NginxSpy\bin\Debug\net8.0-windows\NginxSpy.exe"
    
    if (Test-Path $nginxSpyPath) {
        Write-Host "Launching NginxSpy from: $nginxSpyPath" -ForegroundColor Green
        Start-Process -FilePath $nginxSpyPath
    } else {
        Write-Host "NginxSpy executable not found at: $nginxSpyPath" -ForegroundColor Red
        Write-Host "Please build the project first using 'dotnet build'" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Error launching NginxSpy: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "You can manually run: dotnet run --project NginxSpy" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "After testing, press any key to continue..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# 检查测试后的状态
Write-Host ""
Write-Host "=== Post-Test Status Check ===" -ForegroundColor Green
$remainingProcesses = Get-Process | Where-Object { $_.ProcessName -like "*nginx*" -and $_.ProcessName -ne "NginxSpy" }

if ($remainingProcesses.Count -eq 0) {
    Write-Host "✓ All nginx processes have been stopped successfully!" -ForegroundColor Green
    Write-Host "✓ Single click stop functionality is working correctly!" -ForegroundColor Green
} else {
    Write-Host "Remaining nginx processes:" -ForegroundColor Yellow
    foreach ($proc in $remainingProcesses) {
        Write-Host "  - PID: $($proc.Id), Name: $($proc.ProcessName)" -ForegroundColor White
    }
    Write-Host ""
    Write-Host "If you still need to click twice to stop, please report this issue." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Test completed. Press any key to exit..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")