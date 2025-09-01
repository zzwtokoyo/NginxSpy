# NginxSpy Release 构建脚本
# 用于自动化构建和发布流程

param(
    [string]$OutputPath = ".\Release",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "         NginxSpy 发布构建脚本" -ForegroundColor Cyan  
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# 设置项目路径
$ProjectPath = ".\NginxSpy\NginxSpy.csproj"
$ReleaseDir = $OutputPath

Write-Host "配置信息:" -ForegroundColor Yellow
Write-Host "  项目文件: $ProjectPath" -ForegroundColor Gray
Write-Host "  输出目录: $ReleaseDir" -ForegroundColor Gray
Write-Host "  构建配置: $Configuration" -ForegroundColor Gray
Write-Host "  目标平台: $Runtime" -ForegroundColor Gray
Write-Host ""

# 检查项目文件是否存在
if (-not (Test-Path $ProjectPath)) {
    Write-Host "❌ 错误: 找不到项目文件 $ProjectPath" -ForegroundColor Red
    exit 1
}

try {
    # 1. 清理旧的构建文件
    Write-Host "🧹 清理旧的构建文件..." -ForegroundColor Blue
    if (Test-Path $ReleaseDir) {
        Remove-Item "$ReleaseDir\*.exe" -Force -ErrorAction SilentlyContinue
        Remove-Item "$ReleaseDir\*.pdb" -Force -ErrorAction SilentlyContinue
        Remove-Item "$ReleaseDir\*.dll" -Force -ErrorAction SilentlyContinue
    }
    
    # 清理构建缓存
    & dotnet clean $ProjectPath --configuration $Configuration --verbosity quiet
    Write-Host "✅ 清理完成" -ForegroundColor Green
    
    # 2. 还原NuGet包
    Write-Host "📦 还原NuGet包..." -ForegroundColor Blue
    & dotnet restore $ProjectPath --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "NuGet包还原失败"
    }
    Write-Host "✅ 包还原完成" -ForegroundColor Green
    
    # 3. 构建项目
    Write-Host "🔨 构建项目..." -ForegroundColor Blue
    & dotnet build $ProjectPath --configuration $Configuration --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "项目构建失败"
    }
    Write-Host "✅ 构建完成" -ForegroundColor Green
    
    # 4. 发布应用程序
    Write-Host "🚀 发布应用程序..." -ForegroundColor Blue
    $PublishArgs = @(
        "publish"
        $ProjectPath
        "--configuration", $Configuration
        "--runtime", $Runtime
        "--self-contained", "true"
        "--output", $ReleaseDir
        "/p:PublishSingleFile=true"
        "/p:PublishReadyToRun=true"
        "/p:IncludeNativeLibrariesForSelfExtract=true"
        "--verbosity", "quiet"
    )
    
    & dotnet @PublishArgs
    if ($LASTEXITCODE -ne 0) {
        throw "应用程序发布失败"
    }
    Write-Host "✅ 发布完成" -ForegroundColor Green
    
    # 5. 生成版本信息
    Write-Host "📄 生成版本信息..." -ForegroundColor Blue
    
    # 获取文件信息
    $ExeFile = Get-Item "$ReleaseDir\NginxSpy.exe"
    $FileSizeMB = [math]::Round($ExeFile.Length / 1MB, 2)
    $BuildDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    # 更新README
    $ReadmeContent = @"
# NginxSpy Release v1.0.0

## 📋 应用程序信息

- **应用名称**: NginxSpy
- **版本**: 1.0.0
- **文件大小**: $FileSizeMB MB
- **构建时间**: $BuildDate
- **架构**: Windows x64
- **发布类型**: 自包含单文件发布

## 🚀 快速开始

### 系统要求
- Windows 10/11 (x64)
- .NET 8.0 运行时（已内置，无需额外安装）

### 安装和运行
1. 下载 ``NginxSpy.exe`` 文件
2. 双击运行即可启动应用程序（或使用 ``Run_as_Admin.bat`` 以管理员权限启动）
3. 首次运行建议以管理员权限启动以确保完整功能

## 🛠️ 主要功能

### 📊 实时监控
- 实时监控Nginx进程状态和性能指标
- 显示CPU和内存使用情况统计
- 自动刷新进程信息（5秒间隔）
- 运行状态可视化指示器

### ⚙️ 进程管理
- 启动/停止/重启Nginx进程
- 批量操作多个Nginx实例
- 进程操作日志记录和历史查看
- 自动发现系统中的Nginx进程

### 📝 配置编辑
- 内置Nginx配置文件编辑器
- 语法高亮和实时验证
- 配置文件备份和恢复功能
- 配置段结构树形导航

### 🔧 实例管理
- 添加和管理多个Nginx实例
- 自动检测系统中的Nginx安装
- 实例配置和状态管理
- 自定义实例名称和路径

### ⚙️ 系统设置
- 监控刷新间隔配置
- 界面主题和语言设置
- 日志级别和保留策略
- 数据库自动备份配置

## 🎨 界面特色

- **Material Design** 现代化UI设计
- **响应式布局** 支持窗口缩放调整
- **直观导航** 左侧导航栏快速切换功能
- **状态指示** 实时状态颜色编码
- **工具提示** 丰富的操作提示信息

## 📁 数据存储

应用程序会在以下位置创建数据文件：
- 配置数据库: ``%APPDATA%\NginxSpy\nginxspy.db``
- 日志文件: ``%APPDATA%\NginxSpy\logs\``
- 设置文件: ``%APPDATA%\NginxSpy\settings.json``

## 🛠️ 故障排除

### 常见问题
1. **无法检测到Nginx进程**
   - 确保以管理员权限运行（使用Run_as_Admin.bat）
   - 检查Nginx是否正在运行

2. **配置文件无法保存**
   - 检查文件权限
   - 确保配置文件路径正确

3. **进程操作失败**
   - 确保有足够的系统权限
   - 检查Nginx进程是否响应

### 日志查看
应用程序会记录详细的操作日志，可在以下位置查看：
``````
%APPDATA%\NginxSpy\logs\nginxspy-YYYYMMDD.txt
``````

## 📞 技术支持

如遇到问题，请检查日志文件并提供相关错误信息。

---

**注意**: 此版本为自包含发布版本，包含了所有必要的运行时组件，无需额外安装.NET框架。
"@

    $ReadmeContent | Out-File -FilePath "$ReleaseDir\README.md" -Encoding UTF8
    Write-Host "✅ 版本信息更新完成" -ForegroundColor Green
    
    # 6. 显示发布摘要
    Write-Host ""
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "           发布完成！" -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "发布信息:" -ForegroundColor Yellow
    Write-Host "  输出目录: $ReleaseDir" -ForegroundColor Gray
    Write-Host "  文件大小: $FileSizeMB MB" -ForegroundColor Gray
    Write-Host "  构建时间: $BuildDate" -ForegroundColor Gray
    Write-Host ""
    Write-Host "发布文件:" -ForegroundColor Yellow
    Get-ChildItem $ReleaseDir -File | ForEach-Object {
        $SizeMB = [math]::Round($_.Length / 1MB, 2)
        Write-Host "  $($_.Name) ($SizeMB MB)" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "✅ NginxSpy Release v1.0.0 构建成功！" -ForegroundColor Green
    
} catch {
    Write-Host ""
    Write-Host "❌ 构建失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "请检查错误信息并重试。" -ForegroundColor Yellow
    exit 1
}