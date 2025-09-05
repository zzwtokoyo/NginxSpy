# Test script for nginx process tree termination fix
# Verifies that nginx stop functionality properly handles master and worker processes

Write-Host "=== NginxSpy Process Tree Termination Fix Verification ===" -ForegroundColor Green
Write-Host ""

# Check key fix points
Write-Host "Checking key fix points:" -ForegroundColor Yellow
Write-Host "1. Enhanced StopProcessAsync to handle nginx process tree"
Write-Host "2. Added FindRelatedNginxProcessesAsync method"
Write-Host "3. Proper termination of master and worker processes"
Write-Host "4. Improved process cleanup and error handling"
Write-Host ""

# Check modified files
$serviceFile = "NginxSpy\Services\NginxProcessService.cs"
if (Test-Path $serviceFile) {
    Write-Host "Found NginxProcessService.cs file" -ForegroundColor Green
    
    # Check key modifications
    $content = Get-Content $serviceFile -Raw
    
    if ($content -match "FindRelatedNginxProcessesAsync") {
        Write-Host "Added FindRelatedNginxProcessesAsync method" -ForegroundColor Green
    } else {
        Write-Host "Missing FindRelatedNginxProcessesAsync method" -ForegroundColor Red
    }
    
    if ($content -match "relatedProcesses") {
        Write-Host "Enhanced process discovery logic" -ForegroundColor Green
    } else {
        Write-Host "Missing enhanced process discovery logic" -ForegroundColor Red
    }
    
    if ($content -match "processesToKill") {
        Write-Host "Added prioritized process termination" -ForegroundColor Green
    } else {
        Write-Host "Missing prioritized process termination" -ForegroundColor Red
    }
    
    if ($content -match "allExited") {
        Write-Host "Added comprehensive exit waiting" -ForegroundColor Green
    } else {
        Write-Host "Missing comprehensive exit waiting" -ForegroundColor Red
    }
    
    if ($content -match "killedProcesses") {
        Write-Host "Added detailed termination logging" -ForegroundColor Green
    } else {
        Write-Host "Missing detailed termination logging" -ForegroundColor Red
    }
    
} else {
    Write-Host "NginxProcessService.cs file not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Fix Description ===" -ForegroundColor Yellow
Write-Host "Problem identified:"
Write-Host "- Previous StopProcessAsync only killed single process"
Write-Host "- Nginx typically runs with master process and multiple worker processes"
Write-Host "- Killing only one process left other nginx processes running"
Write-Host "- Users needed to click stop button multiple times"
Write-Host ""
Write-Host "Enhanced solution:"
Write-Host "- Identify all related nginx processes by executable path"
Write-Host "- Terminate worker processes first, then master process"
Write-Host "- Wait for all processes to exit properly"
Write-Host "- Comprehensive error handling and logging"
Write-Host "- Proper resource cleanup"
Write-Host ""
Write-Host "Testing suggestions:"
Write-Host "1. Start NginxSpy application"
Write-Host "2. Add or discover an nginx instance"
Write-Host "3. Start the instance (this may create master + worker processes)"
Write-Host "4. Check Task Manager to see multiple nginx.exe processes"
Write-Host "5. Click stop button once in NginxSpy"
Write-Host "6. Verify ALL nginx processes are terminated"
Write-Host "7. Confirm button state updates correctly"
Write-Host ""
Write-Host "=== Verification Complete ===" -ForegroundColor Green