namespace NginxSpy.Models;

/// <summary>
/// Nginx配置文件模型
/// </summary>
public class NginxConfig
{
    /// <summary>
    /// 配置文件ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的Nginx实例ID
    /// </summary>
    public int InstanceId { get; set; }

    /// <summary>
    /// 配置文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 配置文件内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.Now;

    /// <summary>
    /// 配置是否有效
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// 配置段列表
    /// </summary>
    public List<ConfigSection> Sections { get; set; } = new();

    /// <summary>
    /// 关联的Nginx实例
    /// </summary>
    public NginxInstance? Instance { get; set; }
}

/// <summary>
/// 配置段模型
/// </summary>
public class ConfigSection
{
    /// <summary>
    /// 配置段ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的配置文件ID
    /// </summary>
    public int ConfigFileId { get; set; }

    /// <summary>
    /// 配置段类型
    /// </summary>
    public string SectionType { get; set; } = string.Empty;

    /// <summary>
    /// 配置段名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 配置段内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 父配置段ID
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// 起始行号
    /// </summary>
    public int StartLineNumber { get; set; }

    /// <summary>
    /// 结束行号
    /// </summary>
    public int EndLineNumber { get; set; }

    /// <summary>
    /// 子配置段列表
    /// </summary>
    public List<ConfigSection> Children { get; set; } = new();

    /// <summary>
    /// 父配置段
    /// </summary>
    public ConfigSection? Parent { get; set; }

    /// <summary>
    /// 关联的配置文件
    /// </summary>
    public NginxConfig? ConfigFile { get; set; }
}

/// <summary>
/// 配置段类型常量
/// </summary>
public static class ConfigSectionTypes
{
    public const string Main = "main";
    public const string Events = "events";
    public const string Http = "http";
    public const string Server = "server";
    public const string Location = "location";
    public const string Upstream = "upstream";
    public const string Stream = "stream";
}

/// <summary>
/// 配置指令模型
/// </summary>
public class ConfigDirective
{
    /// <summary>
    /// 指令名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 指令参数
    /// </summary>
    public List<string> Parameters { get; set; } = new();

    /// <summary>
    /// 指令注释
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// 行号
    /// </summary>
    public int LineNumber { get; set; }
}