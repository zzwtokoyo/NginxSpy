# Nginx Spy

一个专为Windows平台设计的Nginx进程监控和管理工具，提供实时进程状态监控、配置文件可视化编辑和进程生命周期管理功能。

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
![License](https://img.shields.io/badge/License-MIT-green)

## 🚀 功能特性

### 📊 实时监控
- **进程状态监控**: 实时显示Nginx进程的运行状态、CPU和内存使用情况
- **性能图表**: 动态展示进程性能指标变化趋势
- **自动发现**: 自动检测系统中运行的Nginx进程

### 🔧 进程管理
- **生命周期控制**: 启动、停止、重启Nginx进程
- **批量操作**: 支持对多个Nginx实例进行批量管理
- **历史记录**: 查看进程启动历史和操作日志
- **默认路径设置**: 配置默认的Nginx可执行文件和配置文件路径

### 📝 配置编辑
- **可视化编辑**: 提供直观的配置文件编辑界面
- **语法验证**: 实时检查配置文件语法错误
- **配置预览**: 实时预览配置文件内容
- **无BOM保存**: 确保配置文件以正确的编码格式保存

### ⚙️ 系统设置
- **应用配置**: 自定义监控间隔、自动启动等参数
- **数据管理**: 管理存储的Nginx实例信息
- **日志查看**: 查看应用程序运行日志和错误信息

## 🛠️ 技术架构

### 技术栈
- **前端**: WPF (.NET 8) + Material Design + LiveCharts2
- **后端**: .NET 8 Class Libraries
- **数据库**: LiteDB 5.x
- **进程管理**: System.Diagnostics + WMI
- **配置解析**: 自定义Nginx配置解析器

### 架构设计
```
┌─────────────────┐
│   WPF用户界面    │
└─────────┬───────┘
          │
┌─────────▼───────┐
│   业务逻辑层     │
├─────────────────┤
│ • 进程监控服务   │
│ • 配置文件解析器 │
│ • 设置服务      │
└─────────┬───────┘
          │
┌─────────▼───────┐
│  LiteDB数据层   │
└─────────────────┘
```

## 📦 安装与使用

### 系统要求
- Windows 10/11 (x64)
- .NET 8.0 Runtime
- 管理员权限（用于进程管理）

### 快速开始

1. **下载发布版本**
   ```
   从 Release 页面下载最新版本的 NginxSpy.exe
   ```

2. **运行应用程序**
   ```
   右键点击 NginxSpy.exe → 以管理员身份运行
   ```

3. **配置默认路径**
   - 打开设置页面
   - 设置默认的Nginx可执行文件路径
   - 设置默认的配置文件路径

4. **添加Nginx实例**
   - 在进程管理页面点击"添加实例"
   - 选择Nginx可执行文件和配置文件
   - 保存实例配置

## 🔨 开发环境搭建

### 前置要求
- Visual Studio 2022 或 Visual Studio Code
- .NET 8.0 SDK
- Git

### 克隆项目
```bash
git clone https://github.com/your-username/nginxspy.git
cd nginxspy
```

### 构建项目
```bash
# 还原依赖包
dotnet restore

# 构建项目
dotnet build

# 运行项目
dotnet run --project NginxSpy
```

### 发布项目
```bash
# 发布为单文件可执行程序
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 📁 项目结构

```
NginxSpy/
├── Commands/              # 命令模式实现
├── Infrastructure/         # 基础设施层
├── Models/                # 数据模型
│   ├── NginxConfig.cs     # Nginx配置模型
│   ├── NginxInstance.cs   # Nginx实例模型
│   └── ProcessLog.cs      # 进程日志模型
├── Services/              # 服务层
│   ├── Interfaces/        # 服务接口
│   ├── NginxConfigService.cs    # 配置文件服务
│   ├── NginxProcessService.cs   # 进程管理服务
│   ├── NginxRepository.cs       # 数据访问服务
│   └── SettingsService.cs       # 设置服务
├── ViewModels/            # 视图模型
│   ├── AddInstanceDialogViewModel.cs
│   ├── ConfigEditorViewModel.cs
│   ├── MainWindowViewModel.cs
│   ├── MonitorViewModel.cs
│   ├── ProcessManagementViewModel.cs
│   └── SettingsViewModel.cs
├── Views/                 # 视图
│   ├── AddInstanceDialog.xaml
│   ├── ConfigEditorView.xaml
│   ├── MainWindow.xaml
│   ├── MonitorView.xaml
│   ├── ProcessManagementView.xaml
│   └── SettingsView.xaml
└── Resources/             # 资源文件
    └── Styles.xaml        # 样式定义
```

## 🎯 主要功能模块

### 1. 主监控页面
- 显示当前运行的Nginx进程数量
- 实时监控CPU和内存使用情况
- 提供快速启停操作按钮
- 展示性能趋势图表

### 2. 进程管理页面
- 列表展示所有Nginx实例
- 单个进程的启动、停止、重启操作
- 查看进程详细信息（PID、路径、配置文件等）
- 进程操作历史记录

### 3. 配置编辑页面
- 加载和解析Nginx配置文件
- 提供语法高亮的编辑器
- 实时配置验证
- 保存配置并重启服务

### 4. 设置页面
- 应用程序参数配置
- 默认路径设置
- 数据库管理
- 日志查看和管理

## 🐛 故障排除

### 常见问题

**Q: 应用程序无法启动Nginx进程**
A: 请确保以管理员权限运行应用程序，并检查Nginx可执行文件路径是否正确。

**Q: 配置文件保存后Nginx启动失败**
A: 检查配置文件语法是否正确，确保没有BOM字符。应用程序已自动处理BOM问题。

**Q: 无法检测到运行中的Nginx进程**
A: 确保Nginx进程正在运行，并且应用程序具有足够的权限访问进程信息。

### 日志文件
应用程序日志保存在以下位置：
- 开发环境: `logs/nginxspy-{date}.txt`
- 发布版本: `Release/logs/nginxspy-{date}.txt`

## 🤝 贡献指南

欢迎提交Issue和Pull Request来改进这个项目！

1. Fork 项目
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 🙏 致谢

- [Material Design In XAML](http://materialdesigninxaml.net/) - 现代化的UI设计
- [LiveCharts2](https://livecharts.dev/) - 强大的图表库
- [LiteDB](https://www.litedb.org/) - 轻量级的NoSQL数据库
- [Serilog](https://serilog.net/) - 结构化日志记录

---

**Nginx Spy** - 让Nginx管理变得简单高效 🚀