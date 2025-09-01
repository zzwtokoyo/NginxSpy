using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging;
using NginxSpy.Models;
using NginxSpy.Services.Interfaces;

namespace NginxSpy.Services;

/// <summary>
/// Nginx数据库操作实现（LiteDB）
/// </summary>
public class NginxRepository : INginxRepository, IDisposable
{
    private readonly ILogger<NginxRepository> _logger;
    private readonly LiteDatabase _database;
    private readonly string _databasePath;

    // 缓存相关
    private readonly ConcurrentDictionary<string, (object Data, DateTime CacheTime)> _cache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    private readonly object _cacheLock = new();

    // 集合名称常量
    private const string InstancesCollection = "nginx_instances";
    private const string ProcessLogsCollection = "process_logs";
    private const string ConfigFilesCollection = "config_files";
    private const string ConfigSectionsCollection = "config_sections";

    public NginxRepository(ILogger<NginxRepository> logger)
    {
        _logger = logger;
        
        // 确保数据目录存在
        var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NginxSpy");
        Directory.CreateDirectory(dataDirectory);
        
        _databasePath = Path.Combine(dataDirectory, "nginxspy.db");
        
        // 初始化LiteDB
        _database = new LiteDatabase(_databasePath);
        
        // 初始化数据库结构
        _ = InitializeDatabaseAsync();
    }

    /// <summary>
    /// 从缓存获取数据
    /// </summary>
    private T? GetFromCache<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            if (DateTime.Now - cached.CacheTime < _cacheExpiration)
            {
                return cached.Data as T;
            }
            else
            {
                // 缓存过期，移除
                _cache.TryRemove(key, out _);
            }
        }
        return null;
    }

    /// <summary>
    /// 设置缓存数据
    /// </summary>
    private void SetCache<T>(string key, T data) where T : class
    {
        _cache[key] = (data, DateTime.Now);
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    private void ClearCache(string? pattern = null)
    {
        if (pattern == null)
        {
            _cache.Clear();
        }
        else
        {
            var keysToRemove = _cache.Keys.Where(k => k.Contains(pattern)).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// 获取所有nginx实例
    /// </summary>
    public async Task<IEnumerable<NginxInstance>> GetAllInstancesAsync()
    {
        const string cacheKey = "all_instances";
        
        // 尝试从缓存获取
        var cached = GetFromCache<List<NginxInstance>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<NginxInstance>(InstancesCollection);
                var instances = collection.FindAll().ToList();
                
                // 设置缓存
                SetCache(cacheKey, instances);
                
                return instances;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有nginx实例时发生错误");
                return Enumerable.Empty<NginxInstance>();
            }
        });
    }

    /// <summary>
    /// 根据ID获取nginx实例
    /// </summary>
    public async Task<NginxInstance?> GetInstanceByIdAsync(int id)
    {
        var cacheKey = $"instance_id_{id}";
        
        // 尝试从缓存获取
        var cached = GetFromCache<NginxInstance>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<NginxInstance>(InstancesCollection);
                var instance = collection.FindById(id);
                
                // 设置缓存
                if (instance != null)
                {
                    SetCache(cacheKey, instance);
                }
                
                return instance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"根据ID获取nginx实例时发生错误，ID: {id}");
                return null;
            }
        });
    }

    /// <summary>
    /// 根据可执行文件路径获取nginx实例
    /// </summary>
    public async Task<NginxInstance?> GetInstanceByExecutablePathAsync(string executablePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<NginxInstance>(InstancesCollection);
                return collection.FindOne(x => x.ExecutablePath.Equals(executablePath, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"根据可执行文件路径获取nginx实例时发生错误，路径: {executablePath}");
                return null;
            }
        });
    }

    /// <summary>
    /// 保存nginx实例
    /// </summary>
    public async Task<int> SaveInstanceAsync(NginxInstance instance)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<NginxInstance>(InstancesCollection);
                var result = collection.Insert(instance);
                
                // 清除相关缓存
                ClearCache("all_instances");
                ClearCache("instance_");
                
                _logger.LogInformation($"保存nginx实例成功，ID: {result.AsInt32}");
                return result.AsInt32;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存nginx实例时发生错误");
                return 0;
            }
        });
    }

    /// <summary>
    /// 更新nginx实例
    /// </summary>
    public async Task<bool> UpdateInstanceAsync(NginxInstance instance)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<NginxInstance>(InstancesCollection);
                var result = collection.Update(instance);
                
                if (result)
                {
                    // 清除相关缓存
                    ClearCache("all_instances");
                    ClearCache($"instance_id_{instance.Id}");
                    ClearCache($"instance_path_{instance.ExecutablePath}");
                }
                
                _logger.LogInformation($"更新nginx实例成功，ID: {instance.Id}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新nginx实例时发生错误，ID: {instance.Id}");
                return false;
            }
        });
    }

    /// <summary>
    /// 删除nginx实例
    /// </summary>
    public async Task<bool> DeleteInstanceAsync(int id)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<NginxInstance>(InstancesCollection);
                var result = collection.Delete(id);
                
                if (result)
                {
                    // 清除相关缓存
                    ClearCache("all_instances");
                    ClearCache($"instance_id_{id}");
                    ClearCache("instance_path_");
                    
                    // 同时删除相关的日志和配置文件
                    _ = Task.Run(async () =>
                    {
                        await DeleteRelatedDataAsync(id);
                    });
                }
                
                _logger.LogInformation($"删除nginx实例成功，ID: {id}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除nginx实例时发生错误，ID: {id}");
                return false;
            }
        });
    }

    /// <summary>
    /// 获取进程操作日志
    /// </summary>
    public async Task<IEnumerable<ProcessLog>> GetProcessLogsAsync(int? instanceId = null, int limit = 100)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<ProcessLog>(ProcessLogsCollection);
                
                var query = collection.Query();
                
                if (instanceId.HasValue)
                {
                    query = query.Where(x => x.InstanceId == instanceId.Value);
                }
                
                return query.OrderByDescending(x => x.Timestamp)
                           .Limit(limit)
                           .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取进程日志时发生错误");
                return Enumerable.Empty<ProcessLog>();
            }
        });
    }

    /// <summary>
    /// 添加进程操作日志
    /// </summary>
    public async Task<int> AddProcessLogAsync(ProcessLog log)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<ProcessLog>(ProcessLogsCollection);
                var result = collection.Insert(log);
                return result.AsInt32;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加进程日志时发生错误");
                return 0;
            }
        });
    }

    /// <summary>
    /// 清理旧的进程日志
    /// </summary>
    public async Task<int> CleanupOldLogsAsync(DateTime olderThan)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<ProcessLog>(ProcessLogsCollection);
                var result = collection.DeleteMany(x => x.Timestamp < olderThan);
                _logger.LogInformation($"清理了 {result} 条旧日志记录");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理旧日志时发生错误");
                return 0;
            }
        });
    }

    /// <summary>
    /// 获取配置文件信息
    /// </summary>
    public async Task<IEnumerable<NginxConfig>> GetConfigFilesAsync(int instanceId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<NginxConfig>(ConfigFilesCollection);
                return collection.Find(x => x.InstanceId == instanceId).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取配置文件时发生错误，实例ID: {instanceId}");
                return Enumerable.Empty<NginxConfig>();
            }
        });
    }

    /// <summary>
    /// 保存配置文件
    /// </summary>
    public async Task<int> SaveConfigFileAsync(NginxConfig config)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<NginxConfig>(ConfigFilesCollection);
                var result = collection.Insert(config);
                return result.AsInt32;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存配置文件时发生错误");
                return 0;
            }
        });
    }

    /// <summary>
    /// 更新配置文件
    /// </summary>
    public async Task<bool> UpdateConfigFileAsync(NginxConfig config)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<NginxConfig>(ConfigFilesCollection);
                return collection.Update(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新配置文件时发生错误，ID: {config.Id}");
                return false;
            }
        });
    }

    /// <summary>
    /// 删除配置文件
    /// </summary>
    public async Task<bool> DeleteConfigFileAsync(int configId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<NginxConfig>(ConfigFilesCollection);
                var result = collection.Delete(configId);
                
                if (result)
                {
                    // 删除相关的配置段
                    var sectionsCollection = _database.GetCollection<ConfigSection>(ConfigSectionsCollection);
                    sectionsCollection.DeleteMany(x => x.ConfigFileId == configId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除配置文件时发生错误，ID: {configId}");
                return false;
            }
        });
    }

    /// <summary>
    /// 获取配置段
    /// </summary>
    public async Task<IEnumerable<ConfigSection>> GetConfigSectionsAsync(int configFileId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<ConfigSection>(ConfigSectionsCollection);
                return collection.Find(x => x.ConfigFileId == configFileId).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取配置段时发生错误，配置文件ID: {configFileId}");
                return Enumerable.Empty<ConfigSection>();
            }
        });
    }

    /// <summary>
    /// 保存配置段
    /// </summary>
    public async Task<int> SaveConfigSectionAsync(ConfigSection section)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<ConfigSection>(ConfigSectionsCollection);
                var result = collection.Insert(section);
                return result.AsInt32;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存配置段时发生错误");
                return 0;
            }
        });
    }

    /// <summary>
    /// 更新配置段
    /// </summary>
    public async Task<bool> UpdateConfigSectionAsync(ConfigSection section)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<ConfigSection>(ConfigSectionsCollection);
                return collection.Update(section);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新配置段时发生错误，ID: {section.Id}");
                return false;
            }
        });
    }

    /// <summary>
    /// 删除配置段
    /// </summary>
    public async Task<bool> DeleteConfigSectionAsync(int sectionId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<ConfigSection>(ConfigSectionsCollection);
                return collection.Delete(sectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除配置段时发生错误，ID: {sectionId}");
                return false;
            }
        });
    }

    /// <summary>
    /// 初始化数据库
    /// </summary>
    public async Task<bool> InitializeDatabaseAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // 创建索引
                var instancesCollection = _database.GetCollection<NginxInstance>(InstancesCollection);
                instancesCollection.EnsureIndex(x => x.ExecutablePath);
                instancesCollection.EnsureIndex(x => x.Name);
                
                var logsCollection = _database.GetCollection<ProcessLog>(ProcessLogsCollection);
                logsCollection.EnsureIndex(x => x.InstanceId);
                logsCollection.EnsureIndex(x => x.Timestamp);
                
                var configsCollection = _database.GetCollection<NginxConfig>(ConfigFilesCollection);
                configsCollection.EnsureIndex(x => x.InstanceId);
                
                var sectionsCollection = _database.GetCollection<ConfigSection>(ConfigSectionsCollection);
                sectionsCollection.EnsureIndex(x => x.ConfigFileId);
                sectionsCollection.EnsureIndex(x => x.ParentId);
                
                _logger.LogInformation("数据库初始化成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库初始化失败");
                return false;
            }
        });
    }

    /// <summary>
    /// 备份数据库
    /// </summary>
    public async Task<bool> BackupDatabaseAsync(string backupPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                File.Copy(_databasePath, backupPath, true);
                _logger.LogInformation($"数据库备份成功，备份路径: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"数据库备份失败，备份路径: {backupPath}");
                return false;
            }
        });
    }

    /// <summary>
    /// 恢复数据库
    /// </summary>
    public async Task<bool> RestoreDatabaseAsync(string backupPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    _logger.LogWarning($"备份文件不存在: {backupPath}");
                    return false;
                }
                
                _database.Dispose();
                File.Copy(backupPath, _databasePath, true);
                
                // 重新初始化数据库连接
                var newDatabase = new LiteDatabase(_databasePath);
                
                _logger.LogInformation($"数据库恢复成功，备份路径: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"数据库恢复失败，备份路径: {backupPath}");
                return false;
            }
        });
    }

    /// <summary>
    /// 删除相关数据
    /// </summary>
    private async Task DeleteRelatedDataAsync(int instanceId)
    {
        await Task.Run(() =>
        {
            try
            {
                // 删除进程日志
                var logsCollection = _database.GetCollection<ProcessLog>(ProcessLogsCollection);
                logsCollection.DeleteMany(x => x.InstanceId == instanceId);
                
                // 删除配置文件和配置段
                var configsCollection = _database.GetCollection<NginxConfig>(ConfigFilesCollection);
                var configs = configsCollection.Find(x => x.InstanceId == instanceId);
                
                var sectionsCollection = _database.GetCollection<ConfigSection>(ConfigSectionsCollection);
                foreach (var config in configs)
                {
                    sectionsCollection.DeleteMany(x => x.ConfigFileId == config.Id);
                }
                
                configsCollection.DeleteMany(x => x.InstanceId == instanceId);
                
                _logger.LogInformation($"删除实例相关数据成功，实例ID: {instanceId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除实例相关数据时发生错误，实例ID: {instanceId}");
            }
        });
    }

    /// <summary>
    /// 批量更新nginx实例
    /// </summary>
    public async Task<int> UpdateInstancesBatchAsync(IEnumerable<NginxInstance> instances)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<NginxInstance>(InstancesCollection);
                var instanceList = instances.ToList();
                int successCount = 0;

                foreach (var instance in instanceList)
                {
                    try
                    {
                        if (collection.Update(instance))
                        {
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"批量更新实例失败，ID: {instance.Id}");
                    }
                }

                // 如果有成功更新的实例，清除缓存
                if (successCount > 0)
                {
                    ClearCache(); // 清除所有缓存
                }
                
                _logger.LogInformation($"批量更新完成，成功: {successCount}/{instanceList.Count}");
                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新nginx实例时发生错误");
                return 0;
            }
        });
    }

    /// <summary>
    /// 批量保存nginx实例
    /// </summary>
    public async Task<int> SaveInstancesBatchAsync(IEnumerable<NginxInstance> instances)
    {
        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<NginxInstance>(InstancesCollection);
                var instanceList = instances.ToList();
                int successCount = 0;

                foreach (var instance in instanceList)
                {
                    try
                    {
                        var result = collection.Insert(instance);
                        if (result.AsInt32 > 0)
                        {
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"批量保存实例失败，名称: {instance.Name}");
                    }
                }

                // 如果有成功保存的实例，清除缓存
                if (successCount > 0)
                {
                    ClearCache(); // 清除所有缓存
                }
                
                _logger.LogInformation($"批量保存完成，成功: {successCount}/{instanceList.Count}");
                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量保存nginx实例时发生错误");
                return 0;
            }
        });
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cache?.Clear();
        _database?.Dispose();
    }
}