# Test nginx stop functionality fix
# This script verifies that nginx process stopping requires only one operation

Write-Host "=== NginxSpy Stop Function Fix Test ==="
Write-Host ""

# Check for running nginx processes
$nginxProcesses = Get-Process | Where-Object {$_.ProcessName -like '*nginx*'}

if ($nginxProcesses.Count -eq 0) {
    Write-Host "No nginx processes currently running"
    Write-Host "Please start an nginx process first, then test the stop function with NginxSpy"
} else {
    Write-Host "Found the following nginx processes:"
    $nginxProcesses | Format-Table ProcessName, Id, StartTime -AutoSize
    
    Write-Host ""
    Write-Host "Test Instructions:"
    Write-Host "1. Start NginxSpy application"
    Write-Host "2. Go to Process Management page and select an nginx process"
    Write-Host "3. Click the 'Stop' button"
    Write-Host "4. Observe if the process stops immediately (no 10-second wait or double-click needed)"
    Write-Host ""
    Write-Host "Before fix: Required 10-second wait or clicking stop button twice"
    Write-Host "After fix: Should stop the process immediately"
}

Write-Host ""
Write-Host "Press any key to exit..."
Read-Host