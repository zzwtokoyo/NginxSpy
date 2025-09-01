using NginxSpy.Models;

namespace NginxSpy.Services.Interfaces;

/// <summary>
/// Nginx进程监控服务接口
/// </summary>
public interface INginxProcessService
{
    /// <summary>
    /// 获取所有nginx实例（包括运行中和已停止的）
    /// </summary>
    /// <returns>nginx实例列表</returns>
    Task<IEnumerable<NginxInstance>> GetAllInstancesAsync();

    /// <summary>
    /// 获取当前运行中的nginx进程
    /// </summary>
    /// <returns>运行中的nginx进程列表</returns>
    Task<IEnumerable<NginxInstance>> GetRunningProcessesAsync();

    /// <summary>
    /// 启动nginx进程
    /// </summary>
    /// <param name="executablePath">nginx可执行文件路径</param>
    /// <param name="configPath">配置文件路径</param>
    /// <returns>启动的进程ID</returns>
    Task<int> StartProcessAsync(string executablePath, string configPath);

    /// <summary>
    /// 停止nginx进程
    /// </summary>
    /// <param name="processId">进程ID</param>
    /// <returns>是否成功停止</returns>
    Task<bool> StopProcessAsync(int processId);

    /// <summary>
    /// 重启nginx进程
    /// </summary>
    /// <param name="processId">进程ID</param>
    /// <returns>新的进程ID</returns>
    Task<int> RestartProcessAsync(int processId);

    /// <summary>
    /// 强制终止nginx进程
    /// </summary>
    /// <param name="processId">进程ID</param>
    /// <returns>是否成功终止</returns>
    Task<bool> KillProcessAsync(int processId);

    /// <summary>
    /// 获取进程的性能信息
    /// </summary>
    /// <param name="processId">进程ID</param>
    /// <returns>性能信息</returns>
    Task<(double cpuUsage, double memoryUsage)> GetProcessPerformanceAsync(int processId);

    /// <summary>
    /// 检查nginx可执行文件是否有效
    /// </summary>
    /// <param name="executablePath">可执行文件路径</param>
    /// <returns>是否有效</returns>
    Task<bool> ValidateNginxExecutableAsync(string executablePath);

    /// <summary>
    /// 扫描系统中的nginx进程
    /// </summary>
    /// <returns>发现的新nginx实例列表</returns>
    Task<IEnumerable<NginxInstance>> ScanForNginxProcessesAsync();

}