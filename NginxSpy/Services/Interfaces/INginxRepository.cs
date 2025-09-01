using NginxSpy.Models;

namespace NginxSpy.Services.Interfaces;

/// <summary>
/// Nginx数据库操作接口
/// </summary>
public interface INginxRepository
{
    /// <summary>
    /// 获取所有nginx实例
    /// </summary>
    /// <returns>nginx实例列表</returns>
    Task<IEnumerable<NginxInstance>> GetAllInstancesAsync();

    /// <summary>
    /// 根据ID获取nginx实例
    /// </summary>
    /// <param name="id">实例ID</param>
    /// <returns>nginx实例</returns>
    Task<NginxInstance?> GetInstanceByIdAsync(int id);

    /// <summary>
    /// 根据可执行文件路径获取nginx实例
    /// </summary>
    /// <param name="executablePath">可执行文件路径</param>
    /// <returns>nginx实例</returns>
    Task<NginxInstance?> GetInstanceByExecutablePathAsync(string executablePath);

    /// <summary>
    /// 保存nginx实例
    /// </summary>
    /// <param name="instance">nginx实例</param>
    /// <returns>保存后的实例ID</returns>
    Task<int> SaveInstanceAsync(NginxInstance instance);

    /// <summary>
    /// 更新nginx实例
    /// </summary>
    /// <param name="instance">nginx实例</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateInstanceAsync(NginxInstance instance);

    /// <summary>
    /// 删除nginx实例
    /// </summary>
    /// <param name="id">实例ID</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteInstanceAsync(int id);

    /// <summary>
    /// 批量更新nginx实例
    /// </summary>
    /// <param name="instances">nginx实例列表</param>
    /// <returns>更新成功的实例数量</returns>
    Task<int> UpdateInstancesBatchAsync(IEnumerable<NginxInstance> instances);

    /// <summary>
    /// 批量保存nginx实例
    /// </summary>
    /// <param name="instances">nginx实例列表</param>
    /// <returns>保存成功的实例数量</returns>
    Task<int> SaveInstancesBatchAsync(IEnumerable<NginxInstance> instances);

    /// <summary>
    /// 获取进程操作日志
    /// </summary>
    /// <param name="instanceId">实例ID，null表示获取所有日志</param>
    /// <param name="limit">限制数量</param>
    /// <returns>进程日志列表</returns>
    Task<IEnumerable<ProcessLog>> GetProcessLogsAsync(int? instanceId = null, int limit = 100);

    /// <summary>
    /// 添加进程操作日志
    /// </summary>
    /// <param name="log">进程日志</param>
    /// <returns>日志ID</returns>
    Task<int> AddProcessLogAsync(ProcessLog log);

    /// <summary>
    /// 清理旧的进程日志
    /// </summary>
    /// <param name="olderThan">清理指定时间之前的日志</param>
    /// <returns>清理的日志数量</returns>
    Task<int> CleanupOldLogsAsync(DateTime olderThan);

    /// <summary>
    /// 获取配置文件信息
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <returns>配置文件列表</returns>
    Task<IEnumerable<NginxConfig>> GetConfigFilesAsync(int instanceId);

    /// <summary>
    /// 保存配置文件
    /// </summary>
    /// <param name="config">配置文件</param>
    /// <returns>配置文件ID</returns>
    Task<int> SaveConfigFileAsync(NginxConfig config);

    /// <summary>
    /// 更新配置文件
    /// </summary>
    /// <param name="config">配置文件</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateConfigFileAsync(NginxConfig config);

    /// <summary>
    /// 删除配置文件
    /// </summary>
    /// <param name="configId">配置文件ID</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteConfigFileAsync(int configId);

    /// <summary>
    /// 获取配置段
    /// </summary>
    /// <param name="configFileId">配置文件ID</param>
    /// <returns>配置段列表</returns>
    Task<IEnumerable<ConfigSection>> GetConfigSectionsAsync(int configFileId);

    /// <summary>
    /// 保存配置段
    /// </summary>
    /// <param name="section">配置段</param>
    /// <returns>配置段ID</returns>
    Task<int> SaveConfigSectionAsync(ConfigSection section);

    /// <summary>
    /// 更新配置段
    /// </summary>
    /// <param name="section">配置段</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateConfigSectionAsync(ConfigSection section);

    /// <summary>
    /// 删除配置段
    /// </summary>
    /// <param name="sectionId">配置段ID</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteConfigSectionAsync(int sectionId);

    /// <summary>
    /// 初始化数据库
    /// </summary>
    /// <returns>是否初始化成功</returns>
    Task<bool> InitializeDatabaseAsync();

    /// <summary>
    /// 备份数据库
    /// </summary>
    /// <param name="backupPath">备份文件路径</param>
    /// <returns>是否备份成功</returns>
    Task<bool> BackupDatabaseAsync(string backupPath);

    /// <summary>
    /// 恢复数据库
    /// </summary>
    /// <param name="backupPath">备份文件路径</param>
    /// <returns>是否恢复成功</returns>
    Task<bool> RestoreDatabaseAsync(string backupPath);
}