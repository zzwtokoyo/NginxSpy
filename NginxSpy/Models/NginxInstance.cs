namespace NginxSpy.Models;

/// <summary>
/// Nginx实例模型
/// </summary>
public class NginxInstance
{
    /// <summary>
    /// 实例ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 实例名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 可执行文件路径
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// 配置文件路径
    /// </summary>
    public string ConfigPath { get; set; } = string.Empty;

    /// <summary>
    /// 工作目录
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后启动时间
    /// </summary>
    public DateTime? LastStarted { get; set; }

    /// <summary>
    /// 是否自动启动
    /// </summary>
    public bool AutoStart { get; set; }

    /// <summary>
    /// 当前进程ID（运行时）
    /// </summary>
    public int? ProcessId { get; set; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public NginxStatus Status { get; set; } = NginxStatus.Stopped;

    /// <summary>
    /// CPU使用率（百分比）
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// 内存使用量（MB）
    /// </summary>
    public double MemoryUsage { get; set; }

    /// <summary>
    /// 运行时长
    /// </summary>
    public TimeSpan? RunningTime => LastStarted.HasValue && Status == NginxStatus.Running 
        ? DateTime.Now - LastStarted.Value 
        : null;
}

/// <summary>
/// Nginx状态枚举
/// </summary>
public enum NginxStatus
{
    /// <summary>
    /// 已停止
    /// </summary>
    Stopped,
    
    /// <summary>
    /// 运行中
    /// </summary>
    Running,
    
    /// <summary>
    /// 启动中
    /// </summary>
    Starting,
    
    /// <summary>
    /// 停止中
    /// </summary>
    Stopping,
    
    /// <summary>
    /// 错误状态
    /// </summary>
    Error
}