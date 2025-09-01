using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NginxSpy.Infrastructure;
using NginxSpy.Services;
using NginxSpy.Services.Interfaces;
using NginxSpy.ViewModels;
using NginxSpy.Views;
using Serilog;
using System.Windows;

namespace NginxSpy;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 配置Serilog日志
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/nginxspy-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // 创建主机
        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(ConfigureServices)
            .Build();

        // 启动主机
        _host.Start();

        // 显示主窗口
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 注册日志服务
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        // 注册服务
        services.AddSingleton<INginxProcessService, NginxProcessService>();
        services.AddSingleton<INginxConfigService, NginxConfigService>();
        services.AddSingleton<INginxRepository, NginxRepository>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // 注册ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MonitorViewModel>();
        services.AddTransient<ProcessManagementViewModel>();
        services.AddTransient<ConfigEditorViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AddInstanceDialogViewModel>();

        // 注册Views
        services.AddSingleton<MainWindow>();
        services.AddTransient<MonitorView>();
        services.AddTransient<ProcessManagementView>();
        services.AddTransient<ConfigEditorView>();
        services.AddTransient<SettingsView>();
    }
}