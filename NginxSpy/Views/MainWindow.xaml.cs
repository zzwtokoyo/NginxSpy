using Microsoft.Extensions.DependencyInjection;
using NginxSpy.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Serilog;

namespace NginxSpy.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger = Log.ForContext<MainWindow>();
    
    public MainWindow(MainWindowViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = viewModel;
        _serviceProvider = serviceProvider;
        _logger.Debug("MainWindow 构造函数开始执行");
        
        // 订阅CurrentView属性变化
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        _logger.Debug("已订阅CurrentView属性变化事件");
        
        // 初始化当前视图
        UpdateCurrentView();
        _logger.Debug("MainWindow 构造函数执行完成");
    }
    
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.CurrentView))
        {
            UpdateCurrentView();
        }
    }
    
    private void UpdateCurrentView()
    {
        var viewModel = (MainWindowViewModel)DataContext;
        var contentControl = FindName("MainContentControl") as ContentControl;
        
        _logger.Debug("UpdateCurrentView 被调用，当前视图: {CurrentView}", viewModel.CurrentView);
        
        if (contentControl != null)
        {
            System.Windows.Controls.UserControl? view = viewModel.CurrentView switch
            {
                "Monitor" => _serviceProvider.GetRequiredService<MonitorView>(),
                "ProcessManagement" => _serviceProvider.GetRequiredService<ProcessManagementView>(),
                "ConfigEditor" => _serviceProvider.GetRequiredService<ConfigEditorView>(),
                "Settings" => _serviceProvider.GetRequiredService<SettingsView>(),
                _ => null
            };
            
            contentControl.Content = view;
            _logger.Debug("视图已设置: {ViewType}", view?.GetType().Name ?? "null");
        }
        else
        {
            _logger.Warning("MainContentControl 未找到");
        }
    }
}