using NginxSpy.ViewModels;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;

namespace NginxSpy.Views;

/// <summary>
/// ConfigEditorView.xaml 的交互逻辑
/// </summary>
public partial class ConfigEditorView : System.Windows.Controls.UserControl
{
    private readonly ConfigEditorViewModel _viewModel;

    public ConfigEditorView(ConfigEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        
        // 订阅跳转事件
        _viewModel.JumpToLineRequested += OnJumpToLineRequested;
        
        // 添加快捷键支持
        KeyDown += OnKeyDown;
        
        // 添加TreeView选中事件处理
        ConfigTreeView.SelectedItemChanged += OnTreeViewSelectedItemChanged;
        
        // 在控件卸载时取消订阅
        Unloaded += (s, e) => {
            _viewModel.JumpToLineRequested -= OnJumpToLineRequested;
            KeyDown -= OnKeyDown;
            ConfigTreeView.SelectedItemChanged -= OnTreeViewSelectedItemChanged;
        };
    }

    /// <summary>
    /// 处理快捷键
    /// </summary>
    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // 按F5刷新模板列表
        if (e.Key == System.Windows.Input.Key.F5)
        {
            e.Handled = true;
            _viewModel.RefreshContextualTemplates();
            
            // 显示调试信息
            var debugInfo = _viewModel.GetDebugInfo();
            System.Windows.MessageBox.Show(debugInfo, "调试信息 - 按F5刷新", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// 处理跳转到指定行的请求
    /// </summary>
    private void OnJumpToLineRequested(int lineNumber)
    {
        try
        {
            var textBox = ConfigTextBox;
            if (textBox == null) return;

            var text = textBox.Text;
            if (string.IsNullOrEmpty(text)) return;

            var lines = text.Split('\n');
            if (lineNumber <= 0 || lineNumber > lines.Length) return;

            // 计算目标行的字符位置
            int charIndex = 0;
            for (int i = 0; i < lineNumber - 1; i++)
            {
                charIndex += lines[i].Length + 1; // +1 for the newline character
            }

            // 设置光标位置并滚动到可见区域
            textBox.Focus();
            textBox.CaretIndex = charIndex;
            textBox.ScrollToLine(lineNumber - 1);
            
            // 选中整行
            var lineLength = lineNumber <= lines.Length ? lines[lineNumber - 1].Length : 0;
            textBox.Select(charIndex, lineLength);
        }
        catch (Exception ex)
         {
             System.Windows.MessageBox.Show($"跳转到行失败: {ex.Message}", "错误", 
                 System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
         }
     }

     /// <summary>
     /// 处理TreeView选中项变化
     /// </summary>
     private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
     {
         if (e.NewValue is NginxSpy.Models.ConfigSection selectedSection)
         {
             _viewModel.SelectedSection = selectedSection;
         }
     }
 }