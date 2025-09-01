namespace NginxSpy.Models;

/// <summary>
/// 进程操作日志模型
/// </summary>
public class ProcessLog
{
    /// <summary>
    /// 日志ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的Nginx实例ID
    /// </summary>
    public int InstanceId { get; set; }

    /// <summary>
    /// 进程ID
    /// </summary>
    public int? ProcessId { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// 操作状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 关联的Nginx实例
    /// </summary>
    public NginxInstance? Instance { get; set; }
}

/// <summary>
/// 进程操作类型常量
/// </summary>
public static class ProcessActions
{
    public const string Start = "Start";
    public const string Stop = "Stop";
    public const string Restart = "Restart";
    public const string Kill = "Kill";
    public const string Discovered = "Discovered";
}

/// <summary>
/// 操作状态常量
/// </summary>
public static class ProcessStatus
{
    public const string Success = "Success";
    public const string Failed = "Failed";
    public const string InProgress = "InProgress";
}