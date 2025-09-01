using Microsoft.Extensions.Logging;
using NginxSpy.Models;
using NginxSpy.Services.Interfaces;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;

namespace NginxSpy.Services;

/// <summary>
/// Nginx进程监控服务实现
/// </summary>
public class NginxProcessService : INginxProcessService
{
    private readonly ILogger<NginxProcessService> _logger;
    private readonly INginxRepository _repository;




    public NginxProcessService(ILogger<NginxProcessService> logger, INginxRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// 获取所有nginx实例
    /// </summary>
    public async Task<IEnumerable<NginxInstance>> GetAllInstancesAsync()
    {
        try
        {
            // 从数据库获取已知实例
            var knownInstances = await _repository.GetAllInstancesAsync();
            var instanceList = knownInstances.ToList();

            // 扫描当前运行的进程
            var runningProcesses = await GetRunningNginxProcessesAsync();

            // 更新已知实例的状态 - 并行处理性能信息获取
            var updateTasks = instanceList.Select(async instance =>
            {
                var runningProcess = runningProcesses.FirstOrDefault(p => 
                    p.MainModule?.FileName?.Equals(instance.ExecutablePath, StringComparison.OrdinalIgnoreCase) == true);

                if (runningProcess != null)
                {
                    instance.Status = NginxStatus.Running;
                    instance.ProcessId = runningProcess.Id;
                    instance.LastStarted = runningProcess.StartTime;

                    // 并行获取性能信息
                    var (cpuUsage, memoryUsage) = await GetProcessPerformanceAsync(runningProcess.Id);
                    instance.CpuUsage = cpuUsage;
                    instance.MemoryUsage = memoryUsage;
                }
                else
                {
                    instance.Status = NginxStatus.Stopped;
                    instance.ProcessId = null;
                    instance.CpuUsage = 0;
                    instance.MemoryUsage = 0;
                }
            });

            await Task.WhenAll(updateTasks);

            // 检查是否有新的nginx进程 - 并行处理新进程信息获取
            var newProcessTasks = runningProcesses
                .Where(process => 
                {
                    var existingInstance = instanceList.FirstOrDefault(i => 
                        i.ExecutablePath.Equals(process.MainModule?.FileName, StringComparison.OrdinalIgnoreCase));
                    return existingInstance == null && process.MainModule?.FileName != null;
                })
                .Select(async process =>
                {
                    // 并行获取配置路径和性能信息
                    var configPathTask = DetectConfigPathAsync(process);
                    var performanceTask = GetProcessPerformanceAsync(process.Id);
                    
                    await Task.WhenAll(configPathTask, performanceTask);
                    
                    var configPath = await configPathTask;
                    var (cpuUsage, memoryUsage) = await performanceTask;

                    var newInstance = new NginxInstance
                    {
                        Name = $"Nginx-{process.Id}",
                        ExecutablePath = process.MainModule?.FileName ?? "",
                        ConfigPath = configPath,
                        WorkingDirectory = Path.GetDirectoryName(process.MainModule?.FileName) ?? "",
                        Status = NginxStatus.Running,
                        ProcessId = process.Id,
                        LastStarted = process.StartTime,
                        CreatedAt = DateTime.Now,
                        CpuUsage = cpuUsage,
                        MemoryUsage = memoryUsage
                    };

                    await _repository.SaveInstanceAsync(newInstance);
                    instanceList.Add(newInstance);

                    _logger.LogInformation($"发现新的nginx进程: {newInstance.Name} (PID: {process.Id})");
                    return newInstance;
                });

            await Task.WhenAll(newProcessTasks);

            return instanceList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取nginx实例时发生错误");
            return Enumerable.Empty<NginxInstance>();
        }
    }

    /// <summary>
    /// 获取运行中的nginx进程
    /// </summary>
    public async Task<IEnumerable<NginxInstance>> GetRunningProcessesAsync()
    {
        var allInstances = await GetAllInstancesAsync();
        return allInstances.Where(i => i.Status == NginxStatus.Running);
    }

    /// <summary>
    /// 启动nginx进程
    /// </summary>
    public async Task<int> StartProcessAsync(string executablePath, string configPath)
    {
        try
        {
            _logger.LogInformation($"启动nginx进程: {executablePath}");

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = $"-c \"{configPath}\"",
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("无法启动nginx进程");
            }

            // 等待进程启动
            await Task.Delay(1000);

            // 检查进程是否仍在运行
            if (process.HasExited)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"nginx进程启动失败: {error}");
            }

            _logger.LogInformation($"nginx进程启动成功，PID: {process.Id}");
            return process.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"启动nginx进程失败: {executablePath}");
            throw;
        }
    }

    /// <summary>
    /// 停止nginx进程
    /// </summary>
    public async Task<bool> StopProcessAsync(int processId)
    {
        try
        {
            _logger.LogInformation($"停止nginx进程，PID: {processId}");

            var process = Process.GetProcessById(processId);
            if (process.HasExited)
            {
                return true;
            }

            // 尝试优雅停止
            process.CloseMainWindow();
            
            // 等待进程退出
            if (await WaitForExitAsync(process, TimeSpan.FromSeconds(10)))
            {
                _logger.LogInformation($"nginx进程已优雅停止，PID: {processId}");
                return true;
            }

            // 如果优雅停止失败，强制终止
            process.Kill();
            await WaitForExitAsync(process, TimeSpan.FromSeconds(5));
            
            _logger.LogInformation($"nginx进程已强制停止，PID: {processId}");
            return true;
        }
        catch (ArgumentException)
        {
            // 进程不存在
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"停止nginx进程失败，PID: {processId}");
            return false;
        }
    }

    /// <summary>
    /// 重启nginx进程
    /// </summary>
    public async Task<int> RestartProcessAsync(int processId)
    {
        try
        {
            // 获取进程信息
            var process = Process.GetProcessById(processId);
            var executablePath = process.MainModule?.FileName ?? throw new InvalidOperationException("无法获取可执行文件路径");
            var configPath = await DetectConfigPathAsync(process);

            // 停止进程
            await StopProcessAsync(processId);

            // 等待一段时间确保进程完全停止
            await Task.Delay(2000);

            // 启动新进程
            return await StartProcessAsync(executablePath, configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"重启nginx进程失败，PID: {processId}");
            throw;
        }
    }

    /// <summary>
    /// 强制终止nginx进程
    /// </summary>
    public async Task<bool> KillProcessAsync(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            process.Kill();
            await WaitForExitAsync(process, TimeSpan.FromSeconds(5));
            return true;
        }
        catch (ArgumentException)
        {
            return true; // 进程不存在
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"强制终止nginx进程失败，PID: {processId}");
            return false;
        }
    }

    /// <summary>
    /// 获取进程性能信息
    /// </summary>
    public async Task<(double cpuUsage, double memoryUsage)> GetProcessPerformanceAsync(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            
            // 内存使用量（MB）
            var memoryUsage = process.WorkingSet64 / 1024.0 / 1024.0;

            // CPU使用率计算
            var cpuUsage = await CalculateCpuUsageAsync(process);

            return (cpuUsage, memoryUsage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"获取进程性能信息失败，PID: {processId}");
            return (0, 0);
        }
    }

    /// <summary>
    /// 验证nginx可执行文件
    /// </summary>
    public async Task<bool> ValidateNginxExecutableAsync(string executablePath)
    {
        try
        {
            if (!File.Exists(executablePath))
                return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "-v",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return false;

            // 添加超时机制，避免无限等待
            var timeout = TimeSpan.FromSeconds(10);
            var exited = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));

            if (!exited)
            {
                _logger.LogWarning($"验证nginx可执行文件超时: {executablePath}");
                process.Kill(); // 确保杀死未响应的进程
                return false;
            }

            // 使用 await 而非 .Result 避免潜在死锁
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(outputTask, errorTask);

            var output = string.Concat(await outputTask, await errorTask);

            return output.Contains("nginx version", StringComparison.OrdinalIgnoreCase);
        }
        catch (Win32Exception winEx)
        {
            _logger.LogWarning(winEx, $"启动nginx可执行文件失败（Win32异常）: {executablePath}");
            return false;
        }
        catch (FileNotFoundException fileEx)
        {
            _logger.LogWarning(fileEx, $"nginx可执行文件未找到: {executablePath}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"验证nginx可执行文件失败: {executablePath}");
            return false;
        }
    }

    /// <summary>
    /// 扫描系统中的nginx进程
    /// </summary>
    public async Task<IEnumerable<NginxInstance>> ScanForNginxProcessesAsync()
    {
        var runningProcesses = await GetRunningNginxProcessesAsync();
        var instances = new List<NginxInstance>();

        foreach (var process in runningProcesses)
        {
            if (process.MainModule?.FileName == null)
                continue;

            var instance = new NginxInstance
            {
                Name = $"Nginx-{process.Id}",
                ExecutablePath = process.MainModule.FileName,
                ConfigPath = await DetectConfigPathAsync(process),
                WorkingDirectory = Path.GetDirectoryName(process.MainModule.FileName) ?? "",
                Status = NginxStatus.Running,
                ProcessId = process.Id,
                LastStarted = process.StartTime,
                CreatedAt = DateTime.Now
            };

            var (cpuUsage, memoryUsage) = await GetProcessPerformanceAsync(process.Id);
            instance.CpuUsage = cpuUsage;
            instance.MemoryUsage = memoryUsage;

            instances.Add(instance);
        }

        return instances;
    }

    /// <summary>
    /// 获取运行中的nginx进程
    /// </summary>
    private async Task<List<Process>> GetRunningNginxProcessesAsync()
    {
        return await Task.Run(() =>
        {
            var nginxProcesses = new List<Process>();
            
            try
            {
                var allProcesses = Process.GetProcesses();
                
                foreach (var process in allProcesses)
                {
                    try
                    {
                        // 检查进程名是否包含nginx（不区分大小写），但排除NginxSpy自身
                        if (process.ProcessName.IndexOf("nginx", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            !process.ProcessName.Equals("NginxSpy", StringComparison.OrdinalIgnoreCase))
                        {
                            // 尝试访问MainModule来验证权限
                            var mainModule = process.MainModule;
                            if (mainModule != null)
                            {
                                nginxProcesses.Add(process);
                                _logger.LogDebug($"发现nginx进程: {process.ProcessName} (PID: {process.Id}, Path: {mainModule.FileName})");
                            }
                            else
                            {
                                // 如果MainModule为null，释放Process对象
                                process.Dispose();
                            }
                        }
                        else
                        {
                            // 如果不是nginx进程，释放Process对象
                            process.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        // 记录无法访问的进程（可能是权限问题）
                        _logger.LogWarning($"无法访问进程 {process.ProcessName} (PID: {process.Id}): {ex.Message}");
                        // 释放Process对象
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统进程列表时发生错误");
            }
            
            _logger.LogInformation($"共发现 {nginxProcesses.Count} 个可访问的nginx进程");
            return nginxProcesses;
        });
    }

    /// <summary>
    /// 检测配置文件路径
    /// </summary>
    private async Task<string> DetectConfigPathAsync(Process process)
    {
        try
        {
            // 尝试从命令行参数中提取配置文件路径
            var commandLine = await GetProcessCommandLineAsync(process.Id);
            var configMatch = Regex.Match(commandLine, @"-c\s+[""']?([^""'\s]+)[""']?", RegexOptions.IgnoreCase);
            
            if (configMatch.Success)
            {
                return configMatch.Groups[1].Value;
            }

            // 默认配置文件路径
            var defaultConfigPath = Path.Combine(Path.GetDirectoryName(process.MainModule?.FileName) ?? "", "conf", "nginx.conf");
            return File.Exists(defaultConfigPath) ? defaultConfigPath : "";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"检测配置文件路径失败，PID: {process.Id}");
            return "";
        }
    }

    /// <summary>
    /// 获取进程命令行
    /// </summary>
    private async Task<string> GetProcessCommandLineAsync(int processId)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
                using var objects = searcher.Get();
                return objects.Cast<ManagementObject>().FirstOrDefault()?["CommandLine"]?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        });
    }

    /// <summary>
    /// 计算CPU使用率
    /// </summary>
    private async Task<double> CalculateCpuUsageAsync(Process process)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            await Task.Delay(1000); // 等待1秒
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            return cpuUsageTotal * 100;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 等待进程退出
    /// </summary>
    private async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        return await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));
    }
}