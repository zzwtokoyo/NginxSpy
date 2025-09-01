namespace NginxSpy.Services.Interfaces;

/// <summary>
/// 应用程序设置服务接口
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// 获取设置值
    /// </summary>
    /// <typeparam name="T">设置值类型</typeparam>
    /// <param name="key">设置键</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>设置值</returns>
    T GetSetting<T>(string key, T defaultValue = default!);

    /// <summary>
    /// 设置值
    /// </summary>
    /// <typeparam name="T">设置值类型</typeparam>
    /// <param name="key">设置键</param>
    /// <param name="value">设置值</param>
    void SetSetting<T>(string key, T value);

    /// <summary>
    /// 删除设置
    /// </summary>
    /// <param name="key">设置键</param>
    /// <returns>是否删除成功</returns>
    bool RemoveSetting(string key);

    /// <summary>
    /// 检查设置是否存在
    /// </summary>
    /// <param name="key">设置键</param>
    /// <returns>是否存在</returns>
    bool HasSetting(string key);

    /// <summary>
    /// 获取所有设置键
    /// </summary>
    /// <returns>设置键列表</returns>
    IEnumerable<string> GetAllKeys();

    /// <summary>
    /// 清空所有设置
    /// </summary>
    void ClearAllSettings();

    /// <summary>
    /// 保存设置到文件
    /// </summary>
    /// <returns>是否保存成功</returns>
    Task<bool> SaveSettingsAsync();

    /// <summary>
    /// 从文件加载设置
    /// </summary>
    /// <returns>是否加载成功</returns>
    Task<bool> LoadSettingsAsync();

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// 导出设置到文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否导出成功</returns>
    Task<bool> ExportSettingsAsync(string filePath);

    /// <summary>
    /// 从文件导入设置
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否导入成功</returns>
    Task<bool> ImportSettingsAsync(string filePath);
}

/// <summary>
/// 应用程序设置常量
/// </summary>
public static class SettingsKeys
{
    // 监控设置
    public const string MonitorRefreshInterval = "Monitor.RefreshInterval";
    public const string MonitorAutoStart = "Monitor.AutoStart";
    public const string MonitorShowNotifications = "Monitor.ShowNotifications";
    
    // 界面设置
    public const string UITheme = "UI.Theme";
    public const string UILanguage = "UI.Language";
    public const string UIWindowState = "UI.WindowState";
    public const string UIWindowSize = "UI.WindowSize";
    public const string UIWindowPosition = "UI.WindowPosition";
    
    // 日志设置
    public const string LogLevel = "Log.Level";
    public const string LogRetentionDays = "Log.RetentionDays";
    public const string LogMaxFileSize = "Log.MaxFileSize";
    
    // 数据库设置
    public const string DatabaseAutoBackup = "Database.AutoBackup";
    public const string DatabaseBackupInterval = "Database.BackupInterval";
    public const string DatabaseBackupRetentionDays = "Database.BackupRetentionDays";
    
    // nginx设置
    public const string NginxDefaultPath = "Nginx.DefaultPath";
    public const string NginxDefaultConfigPath = "Nginx.DefaultConfigPath";
    public const string NginxAutoDiscovery = "Nginx.AutoDiscovery";
    
    // 性能设置
    public const string PerformanceMonitoringEnabled = "Performance.MonitoringEnabled";
    public const string PerformanceDataRetentionHours = "Performance.DataRetentionHours";
}