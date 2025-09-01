using NginxSpy.Commands;
using NginxSpy.Infrastructure;
using NginxSpy.Services.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace NginxSpy.ViewModels;

/// <summary>
/// 主窗口ViewModel
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private string _currentView = "Monitor";
    private string _title = "NginxSpy - Nginx监控管理工具";

    /// <summary>
    /// 当前显示的视图
    /// </summary>
    public string CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    /// <summary>
    /// 窗口标题
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// 导航到监控页面命令
    /// </summary>
    public ICommand NavigateToMonitorCommand { get; }

    /// <summary>
    /// 导航到进程管理页面命令
    /// </summary>
    public ICommand NavigateToProcessManagementCommand { get; }

    /// <summary>
    /// 导航到配置编辑页面命令
    /// </summary>
    public ICommand NavigateToConfigEditorCommand { get; }

    /// <summary>
    /// 导航到设置页面命令
    /// </summary>
    public ICommand NavigateToSettingsCommand { get; }

    /// <summary>
    /// 退出应用程序命令
    /// </summary>
    public ICommand ExitCommand { get; }

    /// <summary>
    /// 关于命令
    /// </summary>
    public ICommand AboutCommand { get; }

    public MainWindowViewModel()
    {
        NavigateToMonitorCommand = new RelayCommand(() => NavigateToView("Monitor"));
        NavigateToProcessManagementCommand = new RelayCommand(() => NavigateToView("ProcessManagement"));
        NavigateToConfigEditorCommand = new RelayCommand(() => NavigateToView("ConfigEditor"));
        NavigateToSettingsCommand = new RelayCommand(() => NavigateToView("Settings"));
        ExitCommand = new RelayCommand(ExitApplication);
        AboutCommand = new RelayCommand(ShowAbout);
    }

    /// <summary>
    /// 导航到指定视图
    /// </summary>
    /// <param name="viewName">视图名称</param>
    private void NavigateToView(string viewName)
    {
        CurrentView = viewName;
        UpdateTitle(viewName);
    }

    /// <summary>
    /// 更新窗口标题
    /// </summary>
    /// <param name="viewName">当前视图名称</param>
    private void UpdateTitle(string viewName)
    {
        var viewTitle = viewName switch
        {
            "Monitor" => "实时监控",
            "ProcessManagement" => "进程管理",
            "ConfigEditor" => "配置编辑",
            "Settings" => "设置",
            _ => "未知页面"
        };

        Title = $"Nginx Spy - {viewTitle}";
    }

    /// <summary>
    /// 退出应用程序
    /// </summary>
    private void ExitApplication()
    {
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>
    /// 显示关于对话框
    /// </summary>
    private void ShowAbout()
    {
        System.Windows.MessageBox.Show(
            "Nginx Spy v1.0.0\n\n" +
            "Windows桌面nginx监控管理工具\n" +
            "提供nginx进程监控、配置管理等功能\n\n" +
            "© 2025 Copyright 钟振伟+AI",
            "关于 Nginx Spy",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }
}