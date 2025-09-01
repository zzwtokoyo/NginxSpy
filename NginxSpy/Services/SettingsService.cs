using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using NginxSpy.Services.Interfaces;

namespace NginxSpy.Services;

/// <summary>
/// 应用程序设置服务实现
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly Dictionary<string, object> _settings;
    private readonly string _settingsFilePath;
    private readonly object _lockObject = new();

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        _settings = new Dictionary<string, object>();
        
        // 设置文件路径
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDataDir = Path.Combine(appDataPath, "NginxSpy");
        Directory.CreateDirectory(appDataDir);
        _settingsFilePath = Path.Combine(appDataDir, "settings.json");
        
        // 初始化默认设置
        InitializeDefaultSettings();
        
        // 加载设置
        _ = LoadSettingsAsync();
    }

    /// <summary>
    /// 获取设置值
    /// </summary>
    public T GetSetting<T>(string key, T defaultValue = default!)
    {
        lock (_lockObject)
        {
            try
            {
                if (_settings.TryGetValue(key, out var value))
                {
                    if (value is JsonElement jsonElement)
                    {
                        return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;
                    }
                    
                    if (value is T directValue)
                    {
                        return directValue;
                    }
                    
                    // 尝试转换类型
                    return (T)Convert.ChangeType(value, typeof(T)) ?? defaultValue;
                }
                
                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"获取设置值失败，键: {key}，使用默认值");
                return defaultValue;
            }
        }
    }

    /// <summary>
    /// 设置值
    /// </summary>
    public void SetSetting<T>(string key, T value)
    {
        lock (_lockObject)
        {
            try
            {
                _settings[key] = value!;
                _logger.LogDebug($"设置值已更新，键: {key}");
                
                // 异步保存设置
                _ = Task.Run(async () => await SaveSettingsAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"设置值失败，键: {key}");
            }
        }
    }

    /// <summary>
    /// 删除设置
    /// </summary>
    public bool RemoveSetting(string key)
    {
        lock (_lockObject)
        {
            try
            {
                var removed = _settings.Remove(key);
                if (removed)
                {
                    _logger.LogDebug($"设置已删除，键: {key}");
                    _ = Task.Run(async () => await SaveSettingsAsync());
                }
                return removed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除设置失败，键: {key}");
                return false;
            }
        }
    }

    /// <summary>
    /// 检查设置是否存在
    /// </summary>
    public bool HasSetting(string key)
    {
        lock (_lockObject)
        {
            return _settings.ContainsKey(key);
        }
    }

    /// <summary>
    /// 获取所有设置键
    /// </summary>
    public IEnumerable<string> GetAllKeys()
    {
        lock (_lockObject)
        {
            return _settings.Keys.ToList();
        }
    }

    /// <summary>
    /// 清空所有设置
    /// </summary>
    public void ClearAllSettings()
    {
        lock (_lockObject)
        {
            try
            {
                _settings.Clear();
                InitializeDefaultSettings();
                _logger.LogInformation("所有设置已清空并重置为默认值");
                _ = Task.Run(async () => await SaveSettingsAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空设置失败");
            }
        }
    }

    /// <summary>
    /// 保存设置到文件
    /// </summary>
    public async Task<bool> SaveSettingsAsync()
    {
        try
        {
            Dictionary<string, object> settingsCopy;
            lock (_lockObject)
            {
                settingsCopy = new Dictionary<string, object>(_settings);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(settingsCopy, options);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            
            _logger.LogDebug($"设置已保存到文件: {_settingsFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"保存设置到文件失败: {_settingsFilePath}");
            return false;
        }
    }

    /// <summary>
    /// 从文件加载设置
    /// </summary>
    public async Task<bool> LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogInformation($"设置文件不存在，使用默认设置: {_settingsFilePath}");
                return true;
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (loadedSettings != null)
            {
                lock (_lockObject)
                {
                    foreach (var kvp in loadedSettings)
                    {
                        _settings[kvp.Key] = kvp.Value;
                    }
                }
            }

            _logger.LogInformation($"设置已从文件加载: {_settingsFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"从文件加载设置失败: {_settingsFilePath}");
            return false;
        }
    }

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    public void ResetToDefaults()
    {
        lock (_lockObject)
        {
            try
            {
                _settings.Clear();
                InitializeDefaultSettings();
                _logger.LogInformation("设置已重置为默认值");
                _ = Task.Run(async () => await SaveSettingsAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置设置失败");
            }
        }
    }

    /// <summary>
    /// 导出设置到文件
    /// </summary>
    public async Task<bool> ExportSettingsAsync(string filePath)
    {
        try
        {
            Dictionary<string, object> settingsCopy;
            lock (_lockObject)
            {
                settingsCopy = new Dictionary<string, object>(_settings);
            }

            var exportData = new
            {
                ExportTime = DateTime.Now,
                Version = "1.0",
                Settings = settingsCopy
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(exportData, options);
            await File.WriteAllTextAsync(filePath, json);
            
            _logger.LogInformation($"设置已导出到文件: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"导出设置失败: {filePath}");
            return false;
        }
    }

    /// <summary>
    /// 从文件导入设置
    /// </summary>
    public async Task<bool> ImportSettingsAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"导入文件不存在: {filePath}");
                return false;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var importData = JsonSerializer.Deserialize<JsonElement>(json);

            if (importData.TryGetProperty("settings", out var settingsElement))
            {
                var importedSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(settingsElement.GetRawText());

                if (importedSettings != null)
                {
                    lock (_lockObject)
                    {
                        foreach (var kvp in importedSettings)
                        {
                            _settings[kvp.Key] = kvp.Value;
                        }
                    }

                    await SaveSettingsAsync();
                    _logger.LogInformation($"设置已从文件导入: {filePath}");
                    return true;
                }
            }

            _logger.LogWarning($"导入文件格式无效: {filePath}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"导入设置失败: {filePath}");
            return false;
        }
    }

    /// <summary>
    /// 初始化默认设置
    /// </summary>
    private void InitializeDefaultSettings()
    {
        // 监控设置
        _settings[SettingsKeys.MonitorRefreshInterval] = 5; // 5秒
        _settings[SettingsKeys.MonitorAutoStart] = true;
        _settings[SettingsKeys.MonitorShowNotifications] = true;
        
        // 界面设置
        _settings[SettingsKeys.UITheme] = "Light";
        _settings[SettingsKeys.UILanguage] = "zh-CN";
        _settings[SettingsKeys.UIWindowState] = "Normal";
        _settings[SettingsKeys.UIWindowSize] = "1200,800";
        _settings[SettingsKeys.UIWindowPosition] = "Center";
        
        // 日志设置
        _settings[SettingsKeys.LogLevel] = "Information";
        _settings[SettingsKeys.LogRetentionDays] = 30;
        _settings[SettingsKeys.LogMaxFileSize] = 10; // 10MB
        
        // 数据库设置
        _settings[SettingsKeys.DatabaseAutoBackup] = true;
        _settings[SettingsKeys.DatabaseBackupInterval] = 24; // 24小时
        _settings[SettingsKeys.DatabaseBackupRetentionDays] = 7;
        
        // nginx设置
        _settings[SettingsKeys.NginxDefaultPath] = @"C:\nginx\nginx.exe";
        _settings[SettingsKeys.NginxDefaultConfigPath] = @"C:\nginx\conf\nginx.conf";
        _settings[SettingsKeys.NginxAutoDiscovery] = true;
        
        // 性能设置
        _settings[SettingsKeys.PerformanceMonitoringEnabled] = true;
        _settings[SettingsKeys.PerformanceDataRetentionHours] = 24;
        
        _logger.LogDebug("默认设置已初始化");
    }
}