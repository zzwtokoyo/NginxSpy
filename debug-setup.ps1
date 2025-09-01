# 设置环境变量以防止Visual Studio自动打开
$env:VSTEST_HOST_DEBUG = '0'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'
$env:VS_DEBUGGER_CAUSAL_LOGGING = '0'
$env:VSJITDEBUGGER = ''

Write-Host "调试环境变量已设置，现在可以在Trae中进行调试而不会打开Visual Studio" -ForegroundColor Green

# 直接运行编译好的程序
Write-Host "正在启动NginxSpy..." -ForegroundColor Yellow
Set-Location "NginxSpy\bin\Debug\net8.0-windows"
.\NginxSpy.exe