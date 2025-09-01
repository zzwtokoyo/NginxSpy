using NginxSpy.Commands;
using NginxSpy.Infrastructure;
using NginxSpy.Models;
using NginxSpy.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using Serilog;

namespace NginxSpy.ViewModels;

/// <summary>
/// 监控页面ViewModel
/// </summary>
public class MonitorViewModel : ViewModelBase, IDisposable
{
    private readonly INginxProcessService _processService;
    private readonly DispatcherTimer _refreshTimer;
    private readonly ILogger _logger = Log.ForContext<MonitorViewModel>();
    
    private int _runningProcessCount;
    private int _totalProcessCount;
    private double _totalCpuUsage;
    private double _totalMemoryUsage;
    private bool _isMonitoring = true;

    /// <summary>
    /// 运行中的进程数量
    /// </summary>
    public int RunningProcessCount
    {
        get => _runningProcessCount;
        set => SetProperty(ref _runningProcessCount, value);
    }

    /// <summary>
    /// 总进程数量
    /// </summary>
    public int TotalProcessCount
    {
        get => _totalProcessCount;
        set => SetProperty(ref _totalProcessCount, value);
    }

    /// <summary>
    /// 总CPU使用率
    /// </summary>
    public double TotalCpuUsage
    {
        get => _totalCpuUsage;
        set => SetProperty(ref _totalCpuUsage, value);
    }

    /// <summary>
    /// 总内存使用量
    /// </summary>
    public double TotalMemoryUsage
    {
        get => _totalMemoryUsage;
        set => SetProperty(ref _totalMemoryUsage, value);
    }

    /// <summary>
    /// 是否正在监控
    /// </summary>
    public bool IsMonitoring
    {
        get => _isMonitoring;
        set => SetProperty(ref _isMonitoring, value);
    }

    /// <summary>
    /// Nginx实例列表
    /// </summary>
    public ObservableCollection<NginxInstance> NginxInstances { get; } = new();

    /// <summary>
    /// 刷新数据命令
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// 启动所有进程命令
    /// </summary>
    public ICommand StartAllCommand { get; }

    /// <summary>
    /// 停止所有进程命令
    /// </summary>
    public ICommand StopAllCommand { get; }

    /// <summary>
    /// 重启所有进程命令
    /// </summary>
    public ICommand RestartAllCommand { get; }

    /// <summary>
    /// 切换监控状态命令
    /// </summary>
    public ICommand ToggleMonitoringCommand { get; }

    public MonitorViewModel(INginxProcessService processService)
    {
        _processService = processService;
        _logger.Debug("MonitorViewModel 构造函数开始执行");
        
        RefreshCommand = new AsyncRelayCommand(RefreshDataAsync);
        StartAllCommand = new AsyncRelayCommand(StartAllProcessesAsync);
        StopAllCommand = new AsyncRelayCommand(StopAllProcessesAsync);
        RestartAllCommand = new AsyncRelayCommand(RestartAllProcessesAsync);
        ToggleMonitoringCommand = new RelayCommand(ToggleMonitoring);

        // 初始化定时器
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5) // 每5秒刷新一次
        };
        _refreshTimer.Tick += async (s, e) => await RefreshDataAsync();
        _refreshTimer.Start();
        _logger.Debug("定时器已启动，间隔5秒");

        // 初始加载数据
        _ = RefreshDataAsync();
        _logger.Debug("MonitorViewModel 构造函数执行完成");
    }

    /// <summary>
    /// 刷新监控数据
    /// </summary>
    private async Task RefreshDataAsync()
    {
        try
        {
            _logger.Debug("开始刷新nginx进程数据...");
            var instances = await _processService.GetAllInstancesAsync();
            _logger.Debug("获取到 {InstanceCount} 个nginx实例", instances.Count());
            
            // 更新UI线程中的集合
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                NginxInstances.Clear();
                foreach (var instance in instances)
                {
                    NginxInstances.Add(instance);
                    _logger.Debug("添加实例: {InstanceName} (PID: {ProcessId}, 状态: {Status})", instance.Name, instance.ProcessId, instance.Status);
                }
            });

            // 更新统计信息
            UpdateStatistics(instances);
            _logger.Debug("数据刷新完成");
        }
        catch (Exception ex)
        {
            // 记录错误日志
            _logger.Error(ex, "刷新数据时发生错误: {ErrorMessage}", ex.Message);
        }
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics(IEnumerable<NginxInstance> instances)
    {
        var instanceList = instances.ToList();
        
        TotalProcessCount = instanceList.Count;
        RunningProcessCount = instanceList.Count(i => i.Status == NginxStatus.Running);
        TotalCpuUsage = instanceList.Where(i => i.Status == NginxStatus.Running).Sum(i => i.CpuUsage);
        TotalMemoryUsage = instanceList.Where(i => i.Status == NginxStatus.Running).Sum(i => i.MemoryUsage);
    }

    /// <summary>
    /// 启动所有进程
    /// </summary>
    private async Task StartAllProcessesAsync()
    {
        try
        {
            var stoppedInstances = NginxInstances.Where(i => i.Status == NginxStatus.Stopped).ToList();
            
            foreach (var instance in stoppedInstances)
            {
                await _processService.StartProcessAsync(instance.ExecutablePath, instance.ConfigPath);
            }
            
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"启动进程时发生错误: {ex.Message}", "错误", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 停止所有进程
    /// </summary>
    private async Task StopAllProcessesAsync()
    {
        try
        {
            var runningInstances = NginxInstances.Where(i => i.Status == NginxStatus.Running).ToList();
            
            foreach (var instance in runningInstances)
            {
                if (instance.ProcessId.HasValue)
                {
                    await _processService.StopProcessAsync(instance.ProcessId.Value);
                }
            }
            
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"停止进程时发生错误: {ex.Message}", "错误", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 重启所有进程
    /// </summary>
    private async Task RestartAllProcessesAsync()
    {
        try
        {
            await StopAllProcessesAsync();
            await Task.Delay(2000); // 等待2秒
            await StartAllProcessesAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"重启进程时发生错误: {ex.Message}", "错误", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 切换监控状态
    /// </summary>
    private void ToggleMonitoring()
    {
        IsMonitoring = !IsMonitoring;
        
        if (IsMonitoring)
        {
            _refreshTimer.Start();
        }
        else
        {
            _refreshTimer.Stop();
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_refreshTimer != null)
        {
            _refreshTimer.Stop();
            _refreshTimer.Tick -= async (s, e) => await RefreshDataAsync();
        }
        _logger.Debug("MonitorViewModel 资源已释放");
    }
}