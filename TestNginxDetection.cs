using Microsoft.Extensions.Logging;
using NginxSpy.Services;
using NginxSpy.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace NginxSpy;

public class TestNginxDetection
{
    public static async Task Main(string[] args)
    {
        // 创建简单的控制台日志
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
        });
        
        var logger = loggerFactory.CreateLogger<NginxProcessService>();
        var repoLogger = loggerFactory.CreateLogger<NginxRepository>();
        
        // 创建服务实例
        var repository = new NginxRepository(repoLogger);
        var processService = new NginxProcessService(logger, repository);
        
        Console.WriteLine("开始检测nginx进程...");
        
        try
        {
            var instances = await processService.GetAllInstancesAsync();
            Console.WriteLine($"检测到 {instances.Count()} 个nginx实例:");
            
            foreach (var instance in instances)
            {
                Console.WriteLine($"- {instance.Name} (PID: {instance.ProcessId}, 状态: {instance.Status})");
                Console.WriteLine($"  路径: {instance.ExecutablePath}");
                Console.WriteLine($"  配置: {instance.ConfigPath}");
                Console.WriteLine();
            }
            
            // 也测试扫描功能
            Console.WriteLine("\n执行系统扫描...");
            var scannedInstances = await processService.ScanForNginxProcessesAsync();
            Console.WriteLine($"扫描发现 {scannedInstances.Count()} 个nginx进程:");
            
            foreach (var instance in scannedInstances)
            {
                Console.WriteLine($"- {instance.Name} (PID: {instance.ProcessId})");
                Console.WriteLine($"  路径: {instance.ExecutablePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"检测过程中发生错误: {ex.Message}");
            Console.WriteLine($"详细信息: {ex}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}