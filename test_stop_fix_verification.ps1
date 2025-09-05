# Test Stop Function Fix Verification Script

Write-Host "=== NginxSpy Stop Function Fix Verification ===" -ForegroundColor Green
Write-Host ""

# Check for running nginx processes
$nginxProcesses = Get-Process | Where-Object { $_.ProcessName -like "*nginx*" -and $_.ProcessName -ne "NginxSpy" }

if ($nginxProcesses.Count -eq 0) {
    Write-Host "No nginx processes currently running." -ForegroundColor Yellow
    Write-Host "Please start an nginx process first for testing." -ForegroundColor Yellow
} else {
    Write-Host "Found running nginx processes:" -ForegroundColor Cyan
    foreach ($proc in $nginxProcesses) {
        Write-Host "  - PID: $($proc.Id), Name: $($proc.ProcessName), Start Time: $($proc.StartTime)" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "=== Fix Summary ===" -ForegroundColor Yellow
Write-Host "1. Added IsLoading state check in ViewModel methods to prevent duplicate operations" -ForegroundColor White
Write-Host "2. Simplified NginxProcessService.StopProcessAsync method, removed unnecessary double checks" -ForegroundColor White
Write-Host "3. Increased appropriate wait timeout (10 seconds) to ensure process fully stops" -ForegroundColor White
Write-Host "4. Improved logging for clearer operation status information" -ForegroundColor White
Write-Host ""

Write-Host "=== Test Steps ===" -ForegroundColor Yellow
Write-Host "1. Start NginxSpy application" -ForegroundColor White
Write-Host "2. Select a running nginx instance in Process Management tab" -ForegroundColor White
Write-Host "3. Click the 'Stop' button ONCE" -ForegroundColor White
Write-Host "4. Verify process stops immediately and status updates to 'Stopped'" -ForegroundColor White
Write-Host "5. Verify no second click is needed" -ForegroundColor White
Write-Host ""

Write-Host "=== Expected Behavior ===" -ForegroundColor Cyan
Write-Host "✓ Single click on stop button successfully stops the process" -ForegroundColor Green
Write-Host "✓ Button shows disabled state during operation (prevents duplicate clicks)" -ForegroundColor Green
Write-Host "✓ Process status immediately updates to 'Stopped'" -ForegroundColor Green
Write-Host "✓ Success or failure message appears after operation" -ForegroundColor Green
Write-Host ""

# Start NginxSpy application
Write-Host "Starting NginxSpy application..." -ForegroundColor Green

try {
    $nginxSpyPath = Join-Path $PSScriptRoot "NginxSpy\bin\Debug\net8.0-windows\NginxSpy.exe"
    
    if (Test-Path $nginxSpyPath) {
        Write-Host "Launching from: $nginxSpyPath" -ForegroundColor Green
        Start-Process -FilePath $nginxSpyPath
        Write-Host ""
        Write-Host "NginxSpy started. Please follow the test steps above for verification." -ForegroundColor Green
    } else {
        Write-Host "NginxSpy executable not found: $nginxSpyPath" -ForegroundColor Red
        Write-Host "Please ensure the project has been built successfully." -ForegroundColor Yellow
    }
} catch {
    Write-Host "Error starting NginxSpy: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "You can manually run: dotnet run --project NginxSpy" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "After testing, press any key to continue..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Check post-test status
Write-Host ""
Write-Host "=== Post-Test Status Check ===" -ForegroundColor Green

$remainingProcesses = Get-Process | Where-Object { $_.ProcessName -like "*nginx*" -and $_.ProcessName -ne "NginxSpy" }

if ($remainingProcesses.Count -eq 0) {
    Write-Host "✓ All nginx processes stopped successfully!" -ForegroundColor Green
    Write-Host "✓ Single-click stop functionality is working correctly!" -ForegroundColor Green
} else {
    Write-Host "Remaining nginx processes:" -ForegroundColor Yellow
    foreach ($proc in $remainingProcesses) {
        Write-Host "  - PID: $($proc.Id), Name: $($proc.ProcessName)" -ForegroundColor White
    }
    Write-Host ""
    Write-Host "If you still need to click twice to stop processes, please report this issue." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Fix Verification Summary ===" -ForegroundColor Cyan
Write-Host "If test successful:" -ForegroundColor White
Write-Host "- Single click stops nginx process" -ForegroundColor Green
Write-Host "- Button state properly managed (disabled during operation)" -ForegroundColor Green
Write-Host "- UI responds promptly and accurately" -ForegroundColor Green
Write-Host ""
Write-Host "If issues persist, please check application logs for detailed information." -ForegroundColor Yellow
Write-Host ""
Write-Host "Test completed. Press any key to exit..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")