using NginxSpy.Commands;
using NginxSpy.Infrastructure;
using NginxSpy.Services.Interfaces;
using System.Windows.Input;

namespace NginxSpy.ViewModels;

/// <summary>
/// 设置页面ViewModel
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly INginxRepository _repository;
    
    // 监控设置
    private int _monitorRefreshInterval;
    private bool _monitorAutoStart;
    private bool _monitorShowNotifications;
    
    // 界面设置
    private string _uiTheme = "Light";
    private string _uiLanguage = "zh-CN";
    
    // 日志设置
    private string _logLevel = "Information";
    private int _logRetentionDays;
    private int _logMaxFileSize;
    
    // 数据库设置
    private bool _databaseAutoBackup;
    private int _databaseBackupInterval;
    private int _databaseBackupRetentionDays;
    
    // nginx设置
    private string _nginxDefaultPath = string.Empty;
    private string _nginxDefaultConfigPath = string.Empty;
    private bool _nginxAutoDiscovery;
    
    // 性能设置
    private bool _performanceMonitoringEnabled;
    private int _performanceDataRetentionHours;
    
    private bool _isLoading;

    #region 属性

    /// <summary>
    /// 监控刷新间隔（秒）
    /// </summary>
    public int MonitorRefreshInterval
    {
        get => _monitorRefreshInterval;
        set => SetProperty(ref _monitorRefreshInterval, value);
    }

    /// <summary>
    /// 监控自动启动
    /// </summary>
    public bool MonitorAutoStart
    {
        get => _monitorAutoStart;
        set => SetProperty(ref _monitorAutoStart, value);
    }

    /// <summary>
    /// 显示通知
    /// </summary>
    public bool MonitorShowNotifications
    {
        get => _monitorShowNotifications;
        set => SetProperty(ref _monitorShowNotifications, value);
    }

    /// <summary>
    /// 界面主题
    /// </summary>
    public string UITheme
    {
        get => _uiTheme;
        set => SetProperty(ref _uiTheme, value);
    }

    /// <summary>
    /// 界面语言
    /// </summary>
    public string UILanguage
    {
        get => _uiLanguage;
        set => SetProperty(ref _uiLanguage, value);
    }

    /// <summary>
    /// 日志级别
    /// </summary>
    public string LogLevel
    {
        get => _logLevel;
        set => SetProperty(ref _logLevel, value);
    }

    /// <summary>
    /// 日志保留天数
    /// </summary>
    public int LogRetentionDays
    {
        get => _logRetentionDays;
        set => SetProperty(ref _logRetentionDays, value);
    }

    /// <summary>
    /// 日志最大文件大小（MB）
    /// </summary>
    public int LogMaxFileSize
    {
        get => _logMaxFileSize;
        set => SetProperty(ref _logMaxFileSize, value);
    }

    /// <summary>
    /// 数据库自动备份
    /// </summary>
    public bool DatabaseAutoBackup
    {
        get => _databaseAutoBackup;
        set => SetProperty(ref _databaseAutoBackup, value);
    }

    /// <summary>
    /// 数据库备份间隔（小时）
    /// </summary>
    public int DatabaseBackupInterval
    {
        get => _databaseBackupInterval;
        set => SetProperty(ref _databaseBackupInterval, value);
    }

    /// <summary>
    /// 数据库备份保留天数
    /// </summary>
    public int DatabaseBackupRetentionDays
    {
        get => _databaseBackupRetentionDays;
        set => SetProperty(ref _databaseBackupRetentionDays, value);
    }

    /// <summary>
    /// nginx默认路径
    /// </summary>
    public string NginxDefaultPath
    {
        get => _nginxDefaultPath;
        set => SetProperty(ref _nginxDefaultPath, value);
    }

    /// <summary>
    /// nginx默认配置路径
    /// </summary>
    public string NginxDefaultConfigPath
    {
        get => _nginxDefaultConfigPath;
        set => SetProperty(ref _nginxDefaultConfigPath, value);
    }

    /// <summary>
    /// nginx自动发现
    /// </summary>
    public bool NginxAutoDiscovery
    {
        get => _nginxAutoDiscovery;
        set => SetProperty(ref _nginxAutoDiscovery, value);
    }

    /// <summary>
    /// 性能监控启用
    /// </summary>
    public bool PerformanceMonitoringEnabled
    {
        get => _performanceMonitoringEnabled;
        set => SetProperty(ref _performanceMonitoringEnabled, value);
    }

    /// <summary>
    /// 性能数据保留小时数
    /// </summary>
    public int PerformanceDataRetentionHours
    {
        get => _performanceDataRetentionHours;
        set => SetProperty(ref _performanceDataRetentionHours, value);
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    #endregion

    #region 命令

    /// <summary>
    /// 保存设置命令
    /// </summary>
    public ICommand SaveSettingsCommand { get; }

    /// <summary>
    /// 重置设置命令
    /// </summary>
    public ICommand ResetSettingsCommand { get; }

    /// <summary>
    /// 导出设置命令
    /// </summary>
    public ICommand ExportSettingsCommand { get; }

    /// <summary>
    /// 导入设置命令
    /// </summary>
    public ICommand ImportSettingsCommand { get; }

    /// <summary>
    /// 备份数据库命令
    /// </summary>
    public ICommand BackupDatabaseCommand { get; }

    /// <summary>
    /// 恢复数据库命令
    /// </summary>
    public ICommand RestoreDatabaseCommand { get; }

    /// <summary>
    /// 清理日志命令
    /// </summary>
    public ICommand CleanupLogsCommand { get; }

    /// <summary>
    /// 浏览nginx路径命令
    /// </summary>
    public ICommand BrowseNginxPathCommand { get; }

    /// <summary>
    /// 浏览配置路径命令
    /// </summary>
    public ICommand BrowseConfigPathCommand { get; }

    #endregion

    public SettingsViewModel(ISettingsService settingsService, INginxRepository repository)
    {
        _settingsService = settingsService;
        _repository = repository;

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        ResetSettingsCommand = new RelayCommand(ResetSettings);
        ExportSettingsCommand = new AsyncRelayCommand(ExportSettingsAsync);
        ImportSettingsCommand = new RelayCommand(ImportSettings);
        BackupDatabaseCommand = new AsyncRelayCommand(BackupDatabaseAsync);
        RestoreDatabaseCommand = new RelayCommand(RestoreDatabase);
        CleanupLogsCommand = new AsyncRelayCommand(CleanupLogsAsync);
        BrowseNginxPathCommand = new RelayCommand(BrowseNginxPath);
        BrowseConfigPathCommand = new RelayCommand(BrowseConfigPath);

        // 加载设置
        LoadSettings();
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            // 监控设置
            MonitorRefreshInterval = _settingsService.GetSetting(SettingsKeys.MonitorRefreshInterval, 5);
            MonitorAutoStart = _settingsService.GetSetting(SettingsKeys.MonitorAutoStart, true);
            MonitorShowNotifications = _settingsService.GetSetting(SettingsKeys.MonitorShowNotifications, true);
            
            // 界面设置
            UITheme = _settingsService.GetSetting(SettingsKeys.UITheme, "Light");
            UILanguage = _settingsService.GetSetting(SettingsKeys.UILanguage, "zh-CN");
            
            // 日志设置
            LogLevel = _settingsService.GetSetting(SettingsKeys.LogLevel, "Information");
            LogRetentionDays = _settingsService.GetSetting(SettingsKeys.LogRetentionDays, 30);
            LogMaxFileSize = _settingsService.GetSetting(SettingsKeys.LogMaxFileSize, 10);
            
            // 数据库设置
            DatabaseAutoBackup = _settingsService.GetSetting(SettingsKeys.DatabaseAutoBackup, true);
            DatabaseBackupInterval = _settingsService.GetSetting(SettingsKeys.DatabaseBackupInterval, 24);
            DatabaseBackupRetentionDays = _settingsService.GetSetting(SettingsKeys.DatabaseBackupRetentionDays, 7);
            
            // nginx设置
            NginxDefaultPath = _settingsService.GetSetting(SettingsKeys.NginxDefaultPath, @"C:\nginx\nginx.exe");
            NginxDefaultConfigPath = _settingsService.GetSetting(SettingsKeys.NginxDefaultConfigPath, @"C:\nginx\conf\nginx.conf");
            NginxAutoDiscovery = _settingsService.GetSetting(SettingsKeys.NginxAutoDiscovery, true);
            
            // 性能设置
            PerformanceMonitoringEnabled = _settingsService.GetSetting(SettingsKeys.PerformanceMonitoringEnabled, true);
            PerformanceDataRetentionHours = _settingsService.GetSetting(SettingsKeys.PerformanceDataRetentionHours, 24);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"加载设置失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        try
        {
            IsLoading = true;

            // 监控设置
            _settingsService.SetSetting(SettingsKeys.MonitorRefreshInterval, MonitorRefreshInterval);
            _settingsService.SetSetting(SettingsKeys.MonitorAutoStart, MonitorAutoStart);
            _settingsService.SetSetting(SettingsKeys.MonitorShowNotifications, MonitorShowNotifications);
            
            // 界面设置
            _settingsService.SetSetting(SettingsKeys.UITheme, UITheme);
            _settingsService.SetSetting(SettingsKeys.UILanguage, UILanguage);
            
            // 日志设置
            _settingsService.SetSetting(SettingsKeys.LogLevel, LogLevel);
            _settingsService.SetSetting(SettingsKeys.LogRetentionDays, LogRetentionDays);
            _settingsService.SetSetting(SettingsKeys.LogMaxFileSize, LogMaxFileSize);
            
            // 数据库设置
            _settingsService.SetSetting(SettingsKeys.DatabaseAutoBackup, DatabaseAutoBackup);
            _settingsService.SetSetting(SettingsKeys.DatabaseBackupInterval, DatabaseBackupInterval);
            _settingsService.SetSetting(SettingsKeys.DatabaseBackupRetentionDays, DatabaseBackupRetentionDays);
            
            // nginx设置
            _settingsService.SetSetting(SettingsKeys.NginxDefaultPath, NginxDefaultPath);
            _settingsService.SetSetting(SettingsKeys.NginxDefaultConfigPath, NginxDefaultConfigPath);
            _settingsService.SetSetting(SettingsKeys.NginxAutoDiscovery, NginxAutoDiscovery);
            
            // 性能设置
            _settingsService.SetSetting(SettingsKeys.PerformanceMonitoringEnabled, PerformanceMonitoringEnabled);
            _settingsService.SetSetting(SettingsKeys.PerformanceDataRetentionHours, PerformanceDataRetentionHours);

            // 保存到文件
            await _settingsService.SaveSettingsAsync();

            System.Windows.MessageBox.Show("设置保存成功", "成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"保存设置失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 重置设置
    /// </summary>
    private void ResetSettings()
    {
        var result = System.Windows.MessageBox.Show(
            "确定要重置所有设置为默认值吗？",
            "确认重置",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;

            _settingsService.ResetToDefaults();
            LoadSettings();

            System.Windows.MessageBox.Show("设置已重置为默认值", "成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"重置设置失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 导出设置
    /// </summary>
    private async Task ExportSettingsAsync()
    {
        // TODO: 实现文件保存对话框
        var filePath = $"nginxspy_settings_{DateTime.Now:yyyyMMddHHmmss}.json";
        
        try
        {
            IsLoading = true;

            var success = await _settingsService.ExportSettingsAsync(filePath);
            
            if (success)
            {
                System.Windows.MessageBox.Show($"设置导出成功\n文件路径: {filePath}", "成功",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("设置导出失败", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"导出设置失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 导入设置
    /// </summary>
    private void ImportSettings()
    {
        // TODO: 实现文件选择对话框
        System.Windows.MessageBox.Show("导入设置功能待实现", "提示",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    /// <summary>
    /// 备份数据库
    /// </summary>
    private async Task BackupDatabaseAsync()
    {
        try
        {
            IsLoading = true;

            var backupPath = $"nginxspy_backup_{DateTime.Now:yyyyMMddHHmmss}.db";
            var success = await _repository.BackupDatabaseAsync(backupPath);
            
            if (success)
            {
                System.Windows.MessageBox.Show($"数据库备份成功\n备份路径: {backupPath}", "成功",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("数据库备份失败", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"备份数据库失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 恢复数据库
    /// </summary>
    private void RestoreDatabase()
    {
        // TODO: 实现文件选择对话框
        System.Windows.MessageBox.Show("恢复数据库功能待实现", "提示",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    /// <summary>
    /// 清理日志
    /// </summary>
    private async Task CleanupLogsAsync()
    {
        var result = System.Windows.MessageBox.Show(
            $"确定要清理 {LogRetentionDays} 天前的日志吗？",
            "确认清理",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;

            var cutoffDate = DateTime.Now.AddDays(-LogRetentionDays);
            var deletedCount = await _repository.CleanupOldLogsAsync(cutoffDate);
            
            System.Windows.MessageBox.Show($"清理完成，删除了 {deletedCount} 条日志记录", "成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"清理日志失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 浏览nginx路径
    /// </summary>
    private void BrowseNginxPath()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择nginx可执行文件",
            Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
            FilterIndex = 1,
            InitialDirectory = string.IsNullOrEmpty(NginxDefaultPath) ? @"C:\" : System.IO.Path.GetDirectoryName(NginxDefaultPath)
        };

        if (openFileDialog.ShowDialog() == true)
        {
            NginxDefaultPath = openFileDialog.FileName;
        }
    }

    /// <summary>
    /// 浏览配置路径
    /// </summary>
    private void BrowseConfigPath()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择nginx配置文件",
            Filter = "配置文件 (*.conf)|*.conf|所有文件 (*.*)|*.*",
            FilterIndex = 1,
            InitialDirectory = string.IsNullOrEmpty(NginxDefaultConfigPath) ? @"C:\" : System.IO.Path.GetDirectoryName(NginxDefaultConfigPath)
        };

        if (openFileDialog.ShowDialog() == true)
        {
            NginxDefaultConfigPath = openFileDialog.FileName;
        }
    }
}