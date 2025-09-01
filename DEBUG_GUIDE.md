# Trae IDE 调试指南

## 在Trae中调试NginxSpy而不打开Visual Studio

### 方法一：使用调试面板（推荐）

1. 在Trae IDE中打开项目
2. 按 `Ctrl+Shift+D` 打开调试面板
3. 在调试配置下拉菜单中选择 **"Launch NginxSpy (No VS)"**
4. 点击绿色的播放按钮或按 `F5` 开始调试
5. 设置断点：在代码行号左侧点击即可设置断点

### 方法二：使用PowerShell脚本

运行以下命令：
```powershell
powershell -ExecutionPolicy Bypass -File .\debug-setup.ps1
```

### 配置说明

我们已经配置了以下文件来确保调试不会打开Visual Studio：

- **`.vscode/launch.json`**: 包含三个调试配置
  - `Launch NginxSpy`: 标准调试配置
  - `Launch NginxSpy (No VS)`: 专门防止VS打开的配置
  - `Attach to NginxSpy`: 附加到运行中的进程

- **`.vscode/settings.json`**: IDE设置，禁用VS自动打开

- **`.vscode/tasks.json`**: 构建任务，包含环境变量设置

- **`.vscode/extensions.json`**: 推荐的C#调试扩展

### 环境变量

以下环境变量被设置来防止Visual Studio自动打开：
- `VSTEST_HOST_DEBUG=0`
- `DOTNET_CLI_TELEMETRY_OPTOUT=1`
- `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1`
- `DOTNET_NOLOGO=1`
- `VS_DEBUGGER_CAUSAL_LOGGING=0`
- `VSJITDEBUGGER=`（空值）

### 调试功能

- ✅ 设置断点
- ✅ 单步调试（F10, F11）
- ✅ 查看变量值
- ✅ 调用堆栈
- ✅ 监视窗口
- ✅ 即时窗口
- ✅ 异常处理

### 故障排除

如果仍然打开Visual Studio：
1. 确保选择了正确的调试配置
2. 重启Trae IDE
3. 检查Windows默认程序关联
4. 使用PowerShell脚本方法

### 注意事项

- 首次调试前请确保项目已成功构建
- 如果遇到权限问题，请以管理员身份运行Trae IDE
- 确保安装了推荐的C#扩展