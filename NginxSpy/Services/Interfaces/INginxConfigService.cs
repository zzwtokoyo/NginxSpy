using NginxSpy.Models;

namespace NginxSpy.Services.Interfaces;

/// <summary>
/// Nginx配置文件解析服务接口
/// </summary>
public interface INginxConfigService
{
    /// <summary>
    /// 解析nginx配置文件
    /// </summary>
    /// <param name="configPath">配置文件路径</param>
    /// <returns>解析后的配置对象</returns>
    Task<NginxConfig> ParseConfigAsync(string configPath);

    /// <summary>
    /// 保存nginx配置文件
    /// </summary>
    /// <param name="config">配置对象</param>
    /// <param name="configPath">保存路径</param>
    /// <returns>是否保存成功</returns>
    Task<bool> SaveConfigAsync(NginxConfig config, string configPath);

    /// <summary>
    /// 验证nginx配置文件语法
    /// </summary>
    /// <param name="config">配置对象</param>
    /// <returns>验证结果</returns>
    Task<ConfigValidationResult> ValidateConfigAsync(NginxConfig config);

    /// <summary>
    /// 验证nginx配置文件语法（通过文件路径）
    /// </summary>
    /// <param name="configPath">配置文件路径</param>
    /// <returns>验证结果</returns>
    Task<ConfigValidationResult> ValidateConfigFileAsync(string configPath);

    /// <summary>
    /// 获取配置文件的指令列表
    /// </summary>
    /// <param name="content">配置文件内容</param>
    /// <returns>指令列表</returns>
    Task<IEnumerable<ConfigDirective>> ParseDirectivesAsync(string content);

    /// <summary>
    /// 格式化配置文件内容
    /// </summary>
    /// <param name="content">原始内容</param>
    /// <returns>格式化后的内容</returns>
    Task<string> FormatConfigContentAsync(string content);

    /// <summary>
    /// 生成配置文件内容
    /// </summary>
    /// <param name="config">配置对象</param>
    /// <returns>配置文件内容</returns>
    Task<string> GenerateConfigContentAsync(NginxConfig config);

    /// <summary>
    /// 从配置段列表生成配置文件内容
    /// </summary>
    /// <param name="sections">配置段列表</param>
    /// <returns>配置文件内容</returns>
    Task<string> GenerateConfigContentAsync(List<ConfigSection> sections);

    /// <summary>
    /// 备份配置文件
    /// </summary>
    /// <param name="configPath">配置文件路径</param>
    /// <returns>备份文件路径</returns>
    Task<string> BackupConfigAsync(string configPath);

    /// <summary>
    /// 恢复配置文件
    /// </summary>
    /// <param name="configPath">配置文件路径</param>
    /// <param name="backupPath">备份文件路径</param>
    /// <returns>是否恢复成功</returns>
    Task<bool> RestoreConfigAsync(string configPath, string backupPath);

    /// <summary>
    /// 获取配置文件模板
    /// </summary>
    /// <param name="templateType">模板类型</param>
    /// <returns>模板内容</returns>
    Task<string> GetConfigTemplateAsync(ConfigTemplateType templateType);

    /// <summary>
    /// 检查配置文件是否存在包含文件
    /// </summary>
    /// <param name="configPath">配置文件路径</param>
    /// <returns>包含的文件列表</returns>
    Task<IEnumerable<string>> GetIncludedFilesAsync(string configPath);

    /// <summary>
    /// 合并配置文件（包括include文件）
    /// </summary>
    /// <param name="mainConfigPath">主配置文件路径</param>
    /// <returns>合并后的配置内容</returns>
    Task<string> MergeConfigFilesAsync(string mainConfigPath);
}

/// <summary>
/// 配置验证结果
/// </summary>
public class ConfigValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误信息列表
    /// </summary>
    public List<ConfigError> Errors { get; set; } = new();

    /// <summary>
    /// 警告信息列表
    /// </summary>
    public List<ConfigWarning> Warnings { get; set; } = new();

    /// <summary>
    /// 验证时间
    /// </summary>
    public DateTime ValidationTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 配置错误信息
/// </summary>
public class ConfigError
{
    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 行号
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// 列号
    /// </summary>
    public int ColumnNumber { get; set; }

    /// <summary>
    /// 错误类型
    /// </summary>
    public ConfigErrorType ErrorType { get; set; }
}

/// <summary>
/// 配置警告信息
/// </summary>
public class ConfigWarning
{
    /// <summary>
    /// 警告消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 行号
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// 警告类型
    /// </summary>
    public ConfigWarningType WarningType { get; set; }
}

/// <summary>
/// 配置错误类型
/// </summary>
public enum ConfigErrorType
{
    /// <summary>
    /// 语法错误
    /// </summary>
    SyntaxError,
    
    /// <summary>
    /// 指令错误
    /// </summary>
    DirectiveError,
    
    /// <summary>
    /// 参数错误
    /// </summary>
    ParameterError,
    
    /// <summary>
    /// 文件不存在
    /// </summary>
    FileNotFound,
    
    /// <summary>
    /// 权限错误
    /// </summary>
    PermissionError
}

/// <summary>
/// 配置警告类型
/// </summary>
public enum ConfigWarningType
{
    /// <summary>
    /// 已弃用的指令
    /// </summary>
    DeprecatedDirective,
    
    /// <summary>
    /// 性能建议
    /// </summary>
    PerformanceSuggestion,
    
    /// <summary>
    /// 安全建议
    /// </summary>
    SecuritySuggestion,
    
    /// <summary>
    /// 配置建议
    /// </summary>
    ConfigurationSuggestion
}

/// <summary>
/// 配置模板类型
/// </summary>
public enum ConfigTemplateType
{
    /// <summary>
    /// 基本配置
    /// </summary>
    Basic,
    
    /// <summary>
    /// Web服务器配置
    /// </summary>
    WebServer,
    
    /// <summary>
    /// 反向代理配置
    /// </summary>
    ReverseProxy,
    
    /// <summary>
    /// 负载均衡配置
    /// </summary>
    LoadBalancer,
    
    /// <summary>
    /// SSL配置
    /// </summary>
    SSL
}