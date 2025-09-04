using NginxSpy.Commands;
using NginxSpy.Infrastructure;
using NginxSpy.Models;
using NginxSpy.Services.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace NginxSpy.ViewModels;

/// <summary>
/// 配置编辑页面ViewModel
/// </summary>
public class ConfigEditorViewModel : ViewModelBase
{
    private readonly INginxConfigService _configService;
    private readonly INginxRepository _repository;
    private readonly INginxProcessService _processService;
    private NginxInstance? _selectedInstance;
    private NginxConfig? _currentConfig;
    private string _configContent = string.Empty;
    private bool _isLoading;
    private bool _hasUnsavedChanges;
    private ConfigSection? _selectedSection;

    /// <summary>
    /// 选中的nginx实例
    /// </summary>
    public NginxInstance? SelectedInstance
    {
        get => _selectedInstance;
        set
        {
            if (SetProperty(ref _selectedInstance, value))
            {
                _ = LoadConfigAsync();
            }
        }
    }

    /// <summary>
    /// 当前配置对象
    /// </summary>
    public NginxConfig? CurrentConfig
    {
        get => _currentConfig;
        set => SetProperty(ref _currentConfig, value);
    }

    /// <summary>
    /// 配置文件内容
    /// </summary>
    public string ConfigContent
    {
        get => _configContent;
        set
        {
            if (SetProperty(ref _configContent, value))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// 是否有未保存的更改
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => SetProperty(ref _hasUnsavedChanges, value);
    }

    /// <summary>
    /// Nginx实例列表
    /// </summary>
    public ObservableCollection<NginxInstance> NginxInstances { get; } = new();

    /// <summary>
    /// 配置段列表
    /// </summary>
    public ObservableCollection<ConfigSection> ConfigSections { get; } = new();

    /// <summary>
    /// 选中的配置段
    /// </summary>
    public ConfigSection? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (SetProperty(ref _selectedSection, value) && value != null)
            {
                JumpToSection(value);
            }
        }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public ConfigValidationResult? ValidationResult { get; private set; }

    /// <summary>
    /// 加载配置命令
    /// </summary>
    public ICommand LoadConfigCommand { get; }

    /// <summary>
    /// 保存配置命令
    /// </summary>
    public ICommand SaveConfigCommand { get; }

    /// <summary>
    /// 验证配置命令
    /// </summary>
    public ICommand ValidateConfigCommand { get; }

    /// <summary>
    /// 格式化配置命令
    /// </summary>
    public ICommand FormatConfigCommand { get; }

    /// <summary>
    /// 备份配置命令
    /// </summary>
    public ICommand BackupConfigCommand { get; }

    /// <summary>
    /// 恢复配置命令
    /// </summary>
    public ICommand RestoreConfigCommand { get; }

    /// <summary>
    /// 重启nginx命令
    /// </summary>
    public ICommand RestartNginxCommand { get; }

    /// <summary>
    /// 新建配置命令
    /// </summary>
    public ICommand NewConfigCommand { get; }

    /// <summary>
    /// 跳转到指定行的事件
    /// </summary>
    public event Action<int>? JumpToLineRequested;

    public ConfigEditorViewModel(INginxConfigService configService, INginxRepository repository, INginxProcessService processService)
    {
        _configService = configService;
        _repository = repository;
        _processService = processService;

        LoadConfigCommand = new AsyncRelayCommand(LoadConfigAsync, () => CanExecuteConfigCommand());
        SaveConfigCommand = new AsyncRelayCommand(SaveConfigAsync, () => CanExecuteConfigCommand());
        ValidateConfigCommand = new AsyncRelayCommand(ValidateConfigAsync, () => CanExecuteConfigCommand());
        FormatConfigCommand = new AsyncRelayCommand(FormatConfigAsync, () => CanExecuteConfigCommand());
        BackupConfigCommand = new AsyncRelayCommand(BackupConfigAsync, () => CanExecuteConfigCommand());
        RestoreConfigCommand = new RelayCommand(RestoreConfig, () => CanExecuteConfigCommand());
        RestartNginxCommand = new AsyncRelayCommand(RestartNginxAsync, () => CanExecuteNginxCommand());
        NewConfigCommand = new AsyncRelayCommand(NewConfigAsync);

        // 初始加载数据
        _ = LoadInstancesAsync();
    }

    /// <summary>
    /// 加载nginx实例列表
    /// </summary>
    private async Task LoadInstancesAsync()
    {
        try
        {
            IsLoading = true;

            var instances = await _repository.GetAllInstancesAsync();
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                NginxInstances.Clear();
                foreach (var instance in instances)
                {
                    NginxInstances.Add(instance);
                }

                // 选择第一个实例
                if (NginxInstances.Count > 0)
                {
                    SelectedInstance = NginxInstances[0];
                }
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"加载实例列表失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 加载配置文件
    /// </summary>
    private async Task LoadConfigAsync()
    {
        if (SelectedInstance == null) return;

        try
        {
            IsLoading = true;

            // 检查是否有未保存的更改
            if (HasUnsavedChanges)
            {
                var result = System.Windows.MessageBox.Show(
                    "当前配置有未保存的更改，是否保存？",
                    "确认",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    await SaveConfigAsync();
                }
                else if (result == System.Windows.MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            // 加载配置文件
            if (File.Exists(SelectedInstance.ConfigPath))
            {
                CurrentConfig = await _configService.ParseConfigAsync(SelectedInstance.ConfigPath);
                ConfigContent = CurrentConfig.Content;
                
                // 加载配置段
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ConfigSections.Clear();
                    foreach (var section in CurrentConfig.Sections)
                    {
                        ConfigSections.Add(section);
                    }
                });

                HasUnsavedChanges = false;
            }
            else
            {
                System.Windows.MessageBox.Show($"配置文件不存在: {SelectedInstance.ConfigPath}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"加载配置文件失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 保存配置文件
    /// </summary>
    private async Task SaveConfigAsync()
    {
        if (CurrentConfig == null || SelectedInstance == null) return;

        try
        {
            IsLoading = true;

            // 更新配置内容
            CurrentConfig.Content = ConfigContent;
            CurrentConfig.LastModified = DateTime.Now;

            // 保存到文件
            var success = await _configService.SaveConfigAsync(CurrentConfig, SelectedInstance.ConfigPath);
            
            if (success)
            {
                // 保存到数据库
                await _repository.UpdateConfigFileAsync(CurrentConfig);
                
                HasUnsavedChanges = false;
                
                System.Windows.MessageBox.Show("配置文件保存成功", "成功",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("配置文件保存失败", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"保存配置文件失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 验证配置文件
    /// </summary>
    private async Task ValidateConfigAsync()
    {
        if (CurrentConfig == null) return;

        try
        {
            IsLoading = true;

            // 更新配置内容
            CurrentConfig.Content = ConfigContent;
            
            // 验证配置
            ValidationResult = await _configService.ValidateConfigAsync(CurrentConfig);
            
            var message = ValidationResult.IsValid 
                ? "配置文件验证通过" 
                : $"配置文件验证失败，发现 {ValidationResult.Errors.Count} 个错误";
            
            var icon = ValidationResult.IsValid 
                ? System.Windows.MessageBoxImage.Information 
                : System.Windows.MessageBoxImage.Warning;
            
            System.Windows.MessageBox.Show(message, "验证结果",
                System.Windows.MessageBoxButton.OK, icon);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"验证配置文件失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 格式化配置文件
    /// </summary>
    private async Task FormatConfigAsync()
    {
        try
        {
            IsLoading = true;

            var formattedContent = await _configService.FormatConfigContentAsync(ConfigContent);
            ConfigContent = formattedContent;
            
            System.Windows.MessageBox.Show("配置文件格式化完成", "成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"格式化配置文件失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 备份配置文件
    /// </summary>
    private async Task BackupConfigAsync()
    {
        if (SelectedInstance == null) return;

        try
        {
            IsLoading = true;

            var backupPath = await _configService.BackupConfigAsync(SelectedInstance.ConfigPath);
            
            System.Windows.MessageBox.Show($"配置文件备份成功\n备份路径: {backupPath}", "成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"备份配置文件失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 恢复配置文件
    /// </summary>
    private void RestoreConfig()
    {
        // TODO: 实现选择备份文件的对话框
        System.Windows.MessageBox.Show("恢复配置功能待实现", "提示",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    /// <summary>
    /// 重启nginx
    /// </summary>
    private async Task RestartNginxAsync()
    {
        if (SelectedInstance?.ProcessId == null) return;

        var result = System.Windows.MessageBox.Show(
            "保存配置并重启nginx进程？",
            "确认重启",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;

            // 先保存配置
            if (HasUnsavedChanges)
            {
                await SaveConfigAsync();
            }

            // 重启nginx进程
            var newProcessId = await _processService.RestartProcessAsync(SelectedInstance.ProcessId.Value);
            
            System.Windows.MessageBox.Show($"nginx重启成功，新PID: {newProcessId}", "成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"重启nginx失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 新建配置
    /// </summary>
    private async Task NewConfigAsync()
    {
        try
        {
            // 检查是否有未保存的更改
            if (HasUnsavedChanges)
            {
                var result = System.Windows.MessageBox.Show(
                    "当前配置有未保存的更改，是否保存？",
                    "确认",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    await SaveConfigAsync();
                }
                else if (result == System.Windows.MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            // 获取基本模板
            var template = await _configService.GetConfigTemplateAsync(ConfigTemplateType.Basic);
            
            CurrentConfig = new NginxConfig
            {
                Content = template,
                LastModified = DateTime.Now,
                IsValid = true
            };
            
            ConfigContent = template;
            HasUnsavedChanges = true;
            
            ConfigSections.Clear();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"创建新配置失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 检查是否可以执行配置命令
    /// </summary>
    private bool CanExecuteConfigCommand()
    {
        return CurrentConfig != null && !IsLoading;
    }

    /// <summary>
    /// 检查是否可以执行nginx命令
    /// </summary>
    private bool CanExecuteNginxCommand()
    {
        return SelectedInstance?.ProcessId != null && !IsLoading;
    }

    /// <summary>
    /// 跳转到指定配置段
    /// </summary>
    private void JumpToSection(ConfigSection section)
    {
        if (section?.StartLineNumber > 0)
        {
            JumpToLineRequested?.Invoke(section.StartLineNumber);
        }
    }
}