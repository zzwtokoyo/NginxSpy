using NginxSpy.Commands;
using NginxSpy.Infrastructure;
using NginxSpy.Models;
using NginxSpy.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace NginxSpy.ViewModels;

/// <summary>
/// 进程管理页面ViewModel
/// </summary>
public class ProcessManagementViewModel : ViewModelBase
{
    private readonly INginxProcessService _processService;
    private readonly INginxRepository _repository;
    private readonly ISettingsService _settingsService;
    private NginxInstance? _selectedInstance;
    private bool _isLoading;

    /// <summary>
    /// 选中的nginx实例
    /// </summary>
    public NginxInstance? SelectedInstance
    {
        get => _selectedInstance;
        set => SetProperty(ref _selectedInstance, value);
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// Nginx实例列表
    /// </summary>
    public ObservableCollection<NginxInstance> NginxInstances { get; } = new();

    /// <summary>
    /// 进程日志列表
    /// </summary>
    public ObservableCollection<ProcessLog> ProcessLogs { get; } = new();

    /// <summary>
    /// 刷新命令
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// 启动进程命令
    /// </summary>
    public ICommand StartProcessCommand { get; }

    /// <summary>
    /// 停止进程命令
    /// </summary>
    public ICommand StopProcessCommand { get; }

    /// <summary>
    /// 重启进程命令
    /// </summary>
    public ICommand RestartProcessCommand { get; }

    /// <summary>
    /// 删除实例命令
    /// </summary>
    public ICommand DeleteInstanceCommand { get; }

    /// <summary>
    /// 添加实例命令
    /// </summary>
    public ICommand AddInstanceCommand { get; }

    /// <summary>
    /// 编辑实例命令
    /// </summary>
    public ICommand EditInstanceCommand { get; }

    /// <summary>
    /// 查看日志命令
    /// </summary>
    public ICommand ViewLogsCommand { get; }

    public ProcessManagementViewModel(INginxProcessService processService, INginxRepository repository, ISettingsService settingsService)
    {
        _processService = processService;
        _repository = repository;
        _settingsService = settingsService;

        RefreshCommand = new AsyncRelayCommand(RefreshDataAsync);
        StartProcessCommand = new AsyncRelayCommand(StartProcessAsync, () => CanExecuteProcessCommand());
        StopProcessCommand = new AsyncRelayCommand(StopProcessAsync, () => CanExecuteProcessCommand());
        RestartProcessCommand = new AsyncRelayCommand(RestartProcessAsync, () => CanExecuteProcessCommand());
        DeleteInstanceCommand = new AsyncRelayCommand(DeleteInstanceAsync, () => CanExecuteInstanceCommand());
        AddInstanceCommand = new RelayCommand(AddInstance);
        EditInstanceCommand = new RelayCommand(EditInstance, () => CanExecuteInstanceCommand());
        ViewLogsCommand = new AsyncRelayCommand(ViewLogsAsync, () => CanExecuteInstanceCommand());

        // 初始加载数据
        _ = RefreshDataAsync();
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    private async Task RefreshDataAsync()
    {
        try
        {
            var instances = await _processService.GetAllInstancesAsync();
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                NginxInstances.Clear();
                foreach (var instance in instances)
                {
                    NginxInstances.Add(instance);
                }
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"刷新数据时发生错误: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 启动进程
    /// </summary>
    private async Task StartProcessAsync()
    {
        if (SelectedInstance == null) return;

        try
        {
            var processId = await _processService.StartProcessAsync(
                SelectedInstance.ExecutablePath, 
                SelectedInstance.ConfigPath);

            // 记录日志
            await _repository.AddProcessLogAsync(new ProcessLog
            {
                InstanceId = SelectedInstance.Id,
                ProcessId = processId,
                Action = ProcessActions.Start,
                Status = ProcessStatus.Success
            });

            // 立即更新当前实例状态
            SelectedInstance.Status = NginxStatus.Running;
            SelectedInstance.ProcessId = processId;
            SelectedInstance.LastStarted = DateTime.Now;

            // 异步刷新数据，不阻塞UI
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000); // 给进程一些时间完全启动
                await RefreshDataAsync();
            });
            
            //System.Windows.MessageBox.Show($"进程启动成功，PID: {processId}", "成功",
             //   System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // 记录错误日志
            await _repository.AddProcessLogAsync(new ProcessLog
            {
                InstanceId = SelectedInstance.Id,
                Action = ProcessActions.Start,
                Status = ProcessStatus.Failed,
                ErrorMessage = ex.Message
            });

            System.Windows.MessageBox.Show($"启动进程失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 停止进程
    /// </summary>
    private async Task StopProcessAsync()
    {
        if (SelectedInstance?.ProcessId == null) return;

        var result = System.Windows.MessageBox.Show(
            $"确定要停止进程 {SelectedInstance.Name} (PID: {SelectedInstance.ProcessId}) 吗？",
            "确认停止",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            var success = await _processService.StopProcessAsync(SelectedInstance.ProcessId.Value);

            // 记录日志
            await _repository.AddProcessLogAsync(new ProcessLog
            {
                InstanceId = SelectedInstance.Id,
                ProcessId = SelectedInstance.ProcessId,
                Action = ProcessActions.Stop,
                Status = success ? ProcessStatus.Success : ProcessStatus.Failed
            });

            // 立即更新当前实例状态，避免等待RefreshDataAsync
            if (success)
            {
                SelectedInstance.Status = NginxStatus.Stopped;
                SelectedInstance.ProcessId = null;
                SelectedInstance.CpuUsage = 0;
                SelectedInstance.MemoryUsage = 0;
            }

            // 异步刷新数据，不阻塞UI
            _ = Task.Run(async () =>
            {
                await Task.Delay(500); // 给进程一些时间完全停止
                await RefreshDataAsync();
            });
            
            if (success)
            {
                //System.Windows.MessageBox.Show("进程停止成功", "成功",
                    //System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("进程停止失败", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            // 记录错误日志
            await _repository.AddProcessLogAsync(new ProcessLog
            {
                InstanceId = SelectedInstance.Id,
                ProcessId = SelectedInstance.ProcessId,
                Action = ProcessActions.Stop,
                Status = ProcessStatus.Failed,
                ErrorMessage = ex.Message
            });

            System.Windows.MessageBox.Show($"停止进程失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 重启进程
    /// </summary>
    private async Task RestartProcessAsync()
    {
        if (SelectedInstance?.ProcessId == null) return;

        var result = System.Windows.MessageBox.Show(
            $"确定要重启进程 {SelectedInstance.Name} (PID: {SelectedInstance.ProcessId}) 吗？",
            "确认重启",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            var newProcessId = await _processService.RestartProcessAsync(SelectedInstance.ProcessId.Value);

            // 记录日志
            await _repository.AddProcessLogAsync(new ProcessLog
            {
                InstanceId = SelectedInstance.Id,
                ProcessId = newProcessId,
                Action = ProcessActions.Restart,
                Status = ProcessStatus.Success
            });

            // 立即更新当前实例状态
            SelectedInstance.Status = NginxStatus.Running;
            SelectedInstance.ProcessId = newProcessId;
            SelectedInstance.LastStarted = DateTime.Now;

            // 异步刷新数据，不阻塞UI
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000); // 给进程一些时间完全启动
                await RefreshDataAsync();
            });
            
            System.Windows.MessageBox.Show($"进程重启成功，新PID: {newProcessId}", "成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // 记录错误日志
            await _repository.AddProcessLogAsync(new ProcessLog
            {
                InstanceId = SelectedInstance.Id,
                ProcessId = SelectedInstance.ProcessId,
                Action = ProcessActions.Restart,
                Status = ProcessStatus.Failed,
                ErrorMessage = ex.Message
            });

            System.Windows.MessageBox.Show($"重启进程失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 删除实例
    /// </summary>
    private async Task DeleteInstanceAsync()
    {
        if (SelectedInstance == null) return;

        var result = System.Windows.MessageBox.Show(
            $"确定要删除实例 {SelectedInstance.Name} 吗？\n\n注意：这将删除实例的所有相关数据，包括日志和配置信息。",
            "确认删除",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            // 如果进程正在运行，先停止
            if (SelectedInstance.Status == NginxStatus.Running && SelectedInstance.ProcessId.HasValue)
            {
                await _processService.StopProcessAsync(SelectedInstance.ProcessId.Value);
            }

            // 删除实例
            var success = await _repository.DeleteInstanceAsync(SelectedInstance.Id);
            
            if (success)
            {
                // 立即从UI中移除实例
                var instanceToRemove = SelectedInstance;
                SelectedInstance = null;
                NginxInstances.Remove(instanceToRemove);
                
                //System.Windows.MessageBox.Show("实例删除成功", "成功",
                    //System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("实例删除失败", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"删除实例失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 添加实例
    /// </summary>
    private async void AddInstance()
    {
        try
        {
            // 创建对话框ViewModel
            var dialogViewModel = new AddInstanceDialogViewModel(_processService, _settingsService);
            
            // 创建并显示对话框
            var dialog = new Views.AddInstanceDialog(dialogViewModel);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            var result = dialog.ShowDialog();
            
            if (result == true && dialogViewModel.CreatedInstance != null)
            {
                // 保存新实例到数据库
                var instanceId = await _repository.SaveInstanceAsync(dialogViewModel.CreatedInstance);
                var success = instanceId > 0;
                
                if (success)
                {
                    // 刷新实例列表
                    await RefreshDataAsync();
                    
                    // 选中新添加的实例
                    var newInstance = NginxInstances.FirstOrDefault(x => x.Name == dialogViewModel.CreatedInstance.Name);
                    if (newInstance != null)
                    {
                        SelectedInstance = newInstance;
                    }
                    
                    //System.Windows.MessageBox.Show($"实例 '{dialogViewModel.CreatedInstance.Name}' 添加成功", "成功",
                        //System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("保存实例失败", "错误",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"添加实例失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 编辑实例
    /// </summary>
    private void EditInstance()
    {
        if (SelectedInstance == null) return;
        
        // TODO: 打开编辑实例对话框
        System.Windows.MessageBox.Show($"编辑实例功能待实现: {SelectedInstance.Name}", "提示",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    /// <summary>
    /// 查看日志
    /// </summary>
    private async Task ViewLogsAsync()
    {
        if (SelectedInstance == null) return;

        try
        {
            IsLoading = true;
            
            var logs = await _repository.GetProcessLogsAsync(SelectedInstance.Id, 50);
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ProcessLogs.Clear();
                foreach (var log in logs)
                {
                    ProcessLogs.Add(log);
                }
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"加载日志失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 检查是否可以执行进程命令
    /// </summary>
    private bool CanExecuteProcessCommand()
    {
        return SelectedInstance != null;
    }

    /// <summary>
    /// 检查是否可以执行实例命令
    /// </summary>
    private bool CanExecuteInstanceCommand()
    {
        return SelectedInstance != null;
    }
}