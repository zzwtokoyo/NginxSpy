using NginxSpy.ViewModels;
using System.Windows;

namespace NginxSpy.Views;

/// <summary>
/// AddInstanceDialog.xaml 的交互逻辑
/// </summary>
public partial class AddInstanceDialog : Window
{
    public AddInstanceDialogViewModel ViewModel { get; }

    public AddInstanceDialog(AddInstanceDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
        
        // 订阅对话框关闭事件
        viewModel.DialogResult += OnDialogResult;
    }

    private void OnDialogResult(object? sender, bool? result)
    {
        DialogResult = result;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        // 取消订阅事件
        ViewModel.DialogResult -= OnDialogResult;
        base.OnClosed(e);
    }
}