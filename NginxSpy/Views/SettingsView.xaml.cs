using NginxSpy.ViewModels;
using System.Windows.Controls;

namespace NginxSpy.Views;

/// <summary>
/// SettingsView.xaml 的交互逻辑
/// </summary>
public partial class SettingsView : System.Windows.Controls.UserControl
{
    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}