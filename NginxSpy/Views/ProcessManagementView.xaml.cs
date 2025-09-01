using NginxSpy.ViewModels;
using System.Windows.Controls;

namespace NginxSpy.Views;

/// <summary>
/// ProcessManagementView.xaml 的交互逻辑
/// </summary>
public partial class ProcessManagementView : System.Windows.Controls.UserControl
{
    public ProcessManagementView(ProcessManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}