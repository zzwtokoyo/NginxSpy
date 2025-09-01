using NginxSpy.ViewModels;
using System.Windows.Controls;

namespace NginxSpy.Views;

/// <summary>
/// MonitorView.xaml 的交互逻辑
/// </summary>
public partial class MonitorView : System.Windows.Controls.UserControl
{
    public MonitorView(MonitorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}