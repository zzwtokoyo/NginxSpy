using Microsoft.Win32;
using NginxSpy.Commands;
using NginxSpy.Infrastructure;
using NginxSpy.Models;
using NginxSpy.Services.Interfaces;
using System.IO;
using System.Windows.Input;

namespace NginxSpy.ViewModels;

/// <summary>
/// 添加实例对话框ViewModel
/// </summary>
public class AddInstanceDialogViewModel : ViewModelBase
{
    private readonly INginxProcessService _processService;
    private readonly ISettingsService _settingsService;
    private string _instanceName = string.Empty;
    private string _executablePath = string.Empty;
    private string _configPath = string.Empty;
    private string _workingDirectory = string.Empty;
    private bool _showValidationResult;
    private string _validationMessage = string.Empty;
    private bool _isValid;

    /// <summary>
    /// 实例名称
    /// </summary>
    public string InstanceName
    {
        get => _instanceName;
        set
        {
            SetProperty(ref _instanceName, value);
            OnPropertyChanged(nameof(CanConfirm));
        }
    }

    /// <summary>
    /// 可执行文件路径
    /// </summary>
    public string ExecutablePath
    {
        get => _executablePath;
        set
        {
            SetProperty(ref _executablePath, value);
            OnPropertyChanged(nameof(CanConfirm));
            
            // 自动设置工作目录
            if (!string.IsNullOrEmpty(value) && File.Exists(value))
            {
                WorkingDirectory = Path.GetDirectoryName(value) ?? string.Empty;
            }
        }
    }

    /// <summary>
    /// 配置文件路径
    /// </summary>
    public string ConfigPath
    {
        get => _configPath;
        set
        {
            SetProperty(ref _configPath, value);
            OnPropertyChanged(nameof(CanConfirm));
        }
    }

    /// <summary>
    /// 工作目录
    /// </summary>
    public string WorkingDirectory
    {
        get => _workingDirectory;
        set => SetProperty(ref _workingDirectory, value);
    }

    /// <summary>
    /// 是否显示验证结果
    /// </summary>
    public bool ShowValidationResult
    {
        get => _showValidationResult;
        set => SetProperty(ref _showValidationResult, value);
    }

    /// <summary>
    /// 验证消息
    /// </summary>
    public string ValidationMessage
    {
        get => _validationMessage;
        set => SetProperty(ref _validationMessage, value);
    }

    /// <summary>
    /// 是否验证通过
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        set => SetProperty(ref _isValid, value);
    }

    /// <summary>
    /// 是否可以确认
    /// </summary>
    public bool CanConfirm => !string.IsNullOrWhiteSpace(InstanceName) &&
                              !string.IsNullOrWhiteSpace(ExecutablePath) &&
                              !string.IsNullOrWhiteSpace(ConfigPath);

    /// <summary>
    /// 浏览可执行文件命令
    /// </summary>
    public ICommand BrowseExecutableCommand { get; }

    /// <summary>
    /// 浏览配置文件命令
    /// </summary>
    public ICommand BrowseConfigCommand { get; }

    /// <summary>
    /// 浏览工作目录命令
    /// </summary>
    public ICommand BrowseWorkingDirectoryCommand { get; }

    /// <summary>
    /// 验证命令
    /// </summary>
    public ICommand ValidateCommand { get; }

    /// <summary>
    /// 确认命令
    /// </summary>
    public ICommand ConfirmCommand { get; }

    /// <summary>
    /// 取消命令
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// 对话框结果事件
    /// </summary>
    public event EventHandler<bool?>? DialogResult;

    /// <summary>
    /// 创建的实例
    /// </summary>
    public NginxInstance? CreatedInstance { get; private set; }

    public AddInstanceDialogViewModel(INginxProcessService processService, ISettingsService settingsService)
    {
        _processService = processService;
        _settingsService = settingsService;

        // 初始化默认路径
        InitializeDefaultPaths();

        BrowseExecutableCommand = new RelayCommand(BrowseExecutable);
        BrowseConfigCommand = new RelayCommand(BrowseConfig);
        BrowseWorkingDirectoryCommand = new RelayCommand(BrowseWorkingDirectory);
        ValidateCommand = new AsyncRelayCommand(ValidateAsync);
        ConfirmCommand = new RelayCommand(Confirm, () => CanConfirm);
        CancelCommand = new RelayCommand(Cancel);
    }

    /// <summary>
    /// 浏览可执行文件
    /// </summary>
    private void BrowseExecutable()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择Nginx可执行文件",
            Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            ExecutablePath = dialog.FileName;
        }
    }

    /// <summary>
    /// 浏览配置文件
    /// </summary>
    private void BrowseConfig()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择Nginx配置文件",
            Filter = "配置文件 (*.conf)|*.conf|所有文件 (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            ConfigPath = dialog.FileName;
        }
    }

    /// <summary>
    /// 浏览工作目录
    /// </summary>
    private void BrowseWorkingDirectory()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择工作目录",
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            WorkingDirectory = dialog.SelectedPath;
        }
    }

    /// <summary>
    /// 验证输入
    /// </summary>
    private async Task ValidateAsync()
    {
        try
        {
            ShowValidationResult = false;
            
            var errors = new List<string>();

            // 验证实例名称
            if (string.IsNullOrWhiteSpace(InstanceName))
            {
                errors.Add("实例名称不能为空");
            }

            // 验证可执行文件
            if (string.IsNullOrWhiteSpace(ExecutablePath))
            {
                errors.Add("可执行文件路径不能为空");
            }
            else if (!File.Exists(ExecutablePath))
            {
                errors.Add("可执行文件不存在");
            }
            else
            {
                var isValidExecutable = await _processService.ValidateNginxExecutableAsync(ExecutablePath);
                if (!isValidExecutable)
                {
                    errors.Add("选择的文件不是有效的Nginx可执行文件");
                }
            }

            // 验证配置文件
            if (string.IsNullOrWhiteSpace(ConfigPath))
            {
                errors.Add("配置文件路径不能为空");
            }
            else if (!File.Exists(ConfigPath))
            {
                errors.Add("配置文件不存在");
            }

            // 验证工作目录
            if (!string.IsNullOrWhiteSpace(WorkingDirectory) && !Directory.Exists(WorkingDirectory))
            {
                errors.Add("工作目录不存在");
            }

            // 显示验证结果
            if (errors.Any())
            {
                IsValid = false;
                ValidationMessage = string.Join("\n", errors);
            }
            else
            {
                IsValid = true;
                ValidationMessage = "验证通过，所有配置都是有效的";
            }

            ShowValidationResult = true;
        }
        catch (Exception ex)
        {
            IsValid = false;
            ValidationMessage = $"验证过程中发生错误: {ex.Message}";
            ShowValidationResult = true;
        }
    }

    /// <summary>
    /// 确认
    /// </summary>
    private void Confirm()
    {
        if (!CanConfirm) return;

        // 创建实例对象
        CreatedInstance = new NginxInstance
        {
            Name = InstanceName.Trim(),
            ExecutablePath = ExecutablePath.Trim(),
            ConfigPath = ConfigPath.Trim(),
            WorkingDirectory = string.IsNullOrWhiteSpace(WorkingDirectory) 
                ? Path.GetDirectoryName(ExecutablePath) ?? string.Empty 
                : WorkingDirectory.Trim(),
            Status = NginxStatus.Stopped,
            CreatedAt = DateTime.Now
        };

        DialogResult?.Invoke(this, true);
    }

    /// <summary>
    /// 取消
    /// </summary>
    private void Cancel()
    {
        DialogResult?.Invoke(this, false);
    }

    /// <summary>
    /// 初始化默认路径
    /// </summary>
    private void InitializeDefaultPaths()
    {
        try
        {
            // 从设置中获取默认路径
            var defaultExecutablePath = _settingsService.GetSetting(SettingsKeys.NginxDefaultPath, @"C:\nginx\nginx.exe");
            var defaultConfigPath = _settingsService.GetSetting(SettingsKeys.NginxDefaultConfigPath, @"C:\nginx\conf\nginx.conf");

            // 如果默认路径存在，则自动填充
            if (!string.IsNullOrEmpty(defaultExecutablePath) && File.Exists(defaultExecutablePath))
            {
                ExecutablePath = defaultExecutablePath;
            }

            if (!string.IsNullOrEmpty(defaultConfigPath) && File.Exists(defaultConfigPath))
            {
                ConfigPath = defaultConfigPath;
            }
        }
        catch (Exception ex)
        {
            // 忽略错误，使用空值
            System.Diagnostics.Debug.WriteLine($"初始化默认路径失败: {ex.Message}");
        }
    }
}