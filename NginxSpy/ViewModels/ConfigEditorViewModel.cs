using NginxSpy.Commands;
using NginxSpy.Infrastructure;
using NginxSpy.Models;
using NginxSpy.Services.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Linq;
using Microsoft.VisualBasic;

namespace NginxSpy.ViewModels;

/// <summary>
/// 配置编辑页面ViewModel
/// </summary>
public class ConfigEditorViewModel : ViewModelBase
{
    private readonly INginxConfigService _configService;
    private readonly INginxRepository _repository;
    private readonly INginxProcessService _processService;
    private NginxInstance? _selectedInstance;
    private NginxConfig? _currentConfig;
    private string _configContent = string.Empty;
    private bool _isLoading;
    private bool _hasUnsavedChanges;
    private ConfigSection? _selectedSection;

    /// <summary>
    /// 选中的nginx实例
    /// </summary>
    public NginxInstance? SelectedInstance
    {
        get => _selectedInstance;
        set
        {
            if (SetProperty(ref _selectedInstance, value))
            {
                _ = LoadConfigAsync();
            }
        }
    }

    /// <summary>
    /// 当前配置对象
    /// </summary>
    public NginxConfig? CurrentConfig
    {
        get => _currentConfig;
        set => SetProperty(ref _currentConfig, value);
    }

    /// <summary>
    /// 配置文件内容
    /// </summary>
    public string ConfigContent
    {
        get => _configContent;
        set
        {
            if (SetProperty(ref _configContent, value))
            {
                HasUnsavedChanges = true;
            }
        }
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
    /// 是否有未保存的更改
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => SetProperty(ref _hasUnsavedChanges, value);
    }

    /// <summary>
    /// Nginx实例列表
    /// </summary>
    public ObservableCollection<NginxInstance> NginxInstances { get; } = new();

    /// <summary>
    /// 配置段列表
    /// </summary>
    public ObservableCollection<ConfigSection> ConfigSections { get; } = new();

    /// <summary>
    /// 选中的配置段
    /// </summary>
    public ConfigSection? SelectedSection
    {
        get => _selectedSection;
        set
        {
            var oldValue = _selectedSection;
            if (SetProperty(ref _selectedSection, value))
            {
                // 调试输出
                System.Diagnostics.Debug.WriteLine($"SelectedSection changed: {oldValue?.SectionType ?? "null"} -> {value?.SectionType ?? "null"}");
                
                // 更新之前选中项的状态
                if (oldValue != null)
                {
                    oldValue.IsSelected = false;
                }
                
                // 设置新选中项的状态
                if (value != null)
                {
                    value.IsSelected = true;
                    JumpToSection(value);
                }
                
                // 更新可用模板列表
                LoadAvailableTemplates();
                UpdateContextualTemplates();
            }
        }
    }

    /// <summary>
    /// 可用的配置段模板
    /// </summary>
    public ObservableCollection<ConfigSectionTemplate> AvailableTemplates { get; } = new();

    private ObservableCollection<ConfigSectionTemplate> _contextualTemplates = new();
    /// <summary>
    /// 基于当前选中节点的可用模板列表
    /// </summary>
    public ObservableCollection<ConfigSectionTemplate> ContextualTemplates
    {
        get => _contextualTemplates;
        set => SetProperty(ref _contextualTemplates, value);
    }

    private ConfigSectionTemplate? _selectedTemplate;
    /// <summary>
    /// 选中的配置段模板
    /// </summary>
    public ConfigSectionTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set => SetProperty(ref _selectedTemplate, value);
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public ConfigValidationResult? ValidationResult { get; private set; }

    /// <summary>
    /// 加载配置命令
    /// </summary>
    public ICommand LoadConfigCommand { get; }

    /// <summary>
    /// 保存配置命令
    /// </summary>
    public ICommand SaveConfigCommand { get; }

    /// <summary>
    /// 验证配置命令
    /// </summary>
    public ICommand ValidateConfigCommand { get; }

    /// <summary>
    /// 格式化配置命令
    /// </summary>
    public ICommand FormatConfigCommand { get; }

    /// <summary>
    /// 备份配置命令
    /// </summary>
    public ICommand BackupConfigCommand { get; }

    /// <summary>
    /// 恢复配置命令
    /// </summary>
    public ICommand RestoreConfigCommand { get; }

    /// <summary>
    /// 重启nginx命令
    /// </summary>
    public ICommand RestartNginxCommand { get; }

    /// <summary>
    /// 新建配置命令
    /// </summary>
    public ICommand NewConfigCommand { get; }

    /// <summary>
    /// 添加配置段命令
    /// </summary>
    public ICommand AddSectionCommand { get; }

    /// <summary>
    /// 删除配置段命令
    /// </summary>
    public ICommand DeleteSectionCommand { get; }

    /// <summary>
    /// 上移配置段命令
    /// </summary>
    public ICommand MoveSectionUpCommand { get; }

    /// <summary>
    /// 下移配置段命令
    /// </summary>
    public ICommand MoveSectionDownCommand { get; }

    /// <summary>
    /// 展开所有配置段命令
    /// </summary>
    public ICommand ExpandAllCommand { get; }

    /// <summary>
    /// 折叠所有配置段命令
    /// </summary>
    public ICommand CollapseAllCommand { get; }

    /// <summary>
    /// 跳转到指定行的事件
    /// </summary>
    public event Action<int>? JumpToLineRequested;

    public ConfigEditorViewModel(INginxConfigService configService, INginxRepository repository, INginxProcessService processService)
    {
        _configService = configService;
        _repository = repository;
        _processService = processService;

        LoadConfigCommand = new AsyncRelayCommand(LoadConfigAsync, () => CanExecuteConfigCommand());
        SaveConfigCommand = new AsyncRelayCommand(SaveConfigAsync, () => CanExecuteConfigCommand());
        ValidateConfigCommand = new AsyncRelayCommand(ValidateConfigAsync, () => CanExecuteConfigCommand());
        FormatConfigCommand = new AsyncRelayCommand(FormatConfigAsync, () => CanExecuteConfigCommand());
        BackupConfigCommand = new AsyncRelayCommand(BackupConfigAsync, () => CanExecuteConfigCommand());
        RestoreConfigCommand = new RelayCommand(RestoreConfig, () => CanExecuteConfigCommand());
        RestartNginxCommand = new AsyncRelayCommand(RestartNginxAsync, () => CanExecuteNginxCommand());
        NewConfigCommand = new AsyncRelayCommand(NewConfigAsync);
        AddSectionCommand = new AsyncRelayCommand(AddSectionAsync, () => CanAddSection());
        DeleteSectionCommand = new AsyncRelayCommand(DeleteSectionAsync, () => CanDeleteSection());
        MoveSectionUpCommand = new RelayCommand(MoveSectionUp, () => CanMoveSectionUp());
        MoveSectionDownCommand = new RelayCommand(MoveSectionDown, () => CanMoveSectionDown());
        ExpandAllCommand = new RelayCommand(ExpandAll);
        CollapseAllCommand = new RelayCommand(CollapseAll);

        // 初始化可用模板
        LoadAvailableTemplates();
        
        // 初始加载数据
        _ = LoadInstancesAsync();
    }

    /// <summary>
    /// 加载nginx实例列表
    /// </summary>
    private async Task LoadInstancesAsync()
    {
        try
        {
            IsLoading = true;

            var instances = await _repository.GetAllInstancesAsync();
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                NginxInstances.Clear();
                foreach (var instance in instances)
                {
                    NginxInstances.Add(instance);
                }

                // 选择第一个实例
                if (NginxInstances.Count > 0)
                {
                    SelectedInstance = NginxInstances[0];
                }
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"加载实例列表失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 加载配置文件
    /// </summary>
    private async Task LoadConfigAsync()
    {
        if (SelectedInstance == null) return;

        try
        {
            IsLoading = true;

            // 检查是否有未保存的更改
            if (HasUnsavedChanges)
            {
                var result = System.Windows.MessageBox.Show(
                    "当前配置有未保存的更改，是否保存？",
                    "确认",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    await SaveConfigAsync();
                }
                else if (result == System.Windows.MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            // 加载配置文件
            if (File.Exists(SelectedInstance.ConfigPath))
            {
                CurrentConfig = await _configService.ParseConfigAsync(SelectedInstance.ConfigPath);
                ConfigContent = CurrentConfig.Content;
                
                // 加载配置段并构建树形结构
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ConfigSections.Clear();
                    var rootSections = BuildSectionTree(CurrentConfig.Sections);
                    foreach (var section in rootSections)
                    {
                        ConfigSections.Add(section);
                    }
                });

                HasUnsavedChanges = false;
            }
            else
            {
                System.Windows.MessageBox.Show($"配置文件不存在: {SelectedInstance.ConfigPath}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"加载配置文件失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 保存配置文件
    /// </summary>
    private async Task SaveConfigAsync()
    {
        if (CurrentConfig == null || SelectedInstance == null) return;

        try
        {
            IsLoading = true;

            // 更新配置内容
            CurrentConfig.Content = ConfigContent;
            CurrentConfig.LastModified = DateTime.Now;

            // 保存到文件
            var success = await _configService.SaveConfigAsync(CurrentConfig, SelectedInstance.ConfigPath);
            
            if (success)
            {
                // 保存到数据库
                await _repository.UpdateConfigFileAsync(CurrentConfig);
                
                HasUnsavedChanges = false;
                
                System.Windows.MessageBox.Show("配置文件保存成功", "成功",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("配置文件保存失败", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"保存配置文件失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 验证配置文件
    /// </summary>
    private async Task ValidateConfigAsync()
    {
        if (CurrentConfig == null) return;

        try
        {
            IsLoading = true;

            // 更新配置内容
            CurrentConfig.Content = ConfigContent;
            
            // 验证配置
            ValidationResult = await _configService.ValidateConfigAsync(CurrentConfig);
            
            var message = ValidationResult.IsValid 
                ? "配置文件验证通过" 
                : $"配置文件验证失败，发现 {ValidationResult.Errors.Count} 个错误";
            
            var icon = ValidationResult.IsValid 
                ? System.Windows.MessageBoxImage.Information 
                : System.Windows.MessageBoxImage.Warning;
            
            System.Windows.MessageBox.Show(message, "验证结果",
                System.Windows.MessageBoxButton.OK, icon);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"验证配置文件失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 格式化配置文件
    /// </summary>
    private async Task FormatConfigAsync()
    {
        try
        {
            IsLoading = true;

            var formattedContent = await _configService.FormatConfigContentAsync(ConfigContent);
            ConfigContent = formattedContent;
            
            System.Windows.MessageBox.Show("配置文件格式化完成", "成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"格式化配置文件失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 备份配置文件
    /// </summary>
    private async Task BackupConfigAsync()
    {
        if (SelectedInstance == null) return;

        try
        {
            IsLoading = true;

            var backupPath = await _configService.BackupConfigAsync(SelectedInstance.ConfigPath);
            
            System.Windows.MessageBox.Show($"配置文件备份成功\n备份路径: {backupPath}", "成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"备份配置文件失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 恢复配置文件
    /// </summary>
    private void RestoreConfig()
    {
        // TODO: 实现选择备份文件的对话框
        System.Windows.MessageBox.Show("恢复配置功能待实现", "提示",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    /// <summary>
    /// 重启nginx
    /// </summary>
    private async Task RestartNginxAsync()
    {
        if (SelectedInstance?.ProcessId == null) return;

        var result = System.Windows.MessageBox.Show(
            "保存配置并重启nginx进程？",
            "确认重启",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;

            // 先保存配置
            if (HasUnsavedChanges)
            {
                await SaveConfigAsync();
            }

            // 重启nginx进程
            var newProcessId = await _processService.RestartProcessAsync(SelectedInstance.ProcessId.Value);
            
            System.Windows.MessageBox.Show($"nginx重启成功，新PID: {newProcessId}", "成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"重启nginx失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 新建配置
    /// </summary>
    private async Task NewConfigAsync()
    {
        try
        {
            // 检查是否有未保存的更改
            if (HasUnsavedChanges)
            {
                var result = System.Windows.MessageBox.Show(
                    "当前配置有未保存的更改，是否保存？",
                    "确认",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    await SaveConfigAsync();
                }
                else if (result == System.Windows.MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            // 获取基本模板
            var template = await _configService.GetConfigTemplateAsync(ConfigTemplateType.Basic);
            
            CurrentConfig = new NginxConfig
            {
                Content = template,
                LastModified = DateTime.Now,
                IsValid = true
            };
            
            ConfigContent = template;
            HasUnsavedChanges = true;
            
            ConfigSections.Clear();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"创建新配置失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 检查是否可以执行配置命令
    /// </summary>
    private bool CanExecuteConfigCommand()
    {
        return CurrentConfig != null && !IsLoading;
    }

    /// <summary>
    /// 检查是否可以执行nginx命令
    /// </summary>
    private bool CanExecuteNginxCommand()
    {
        return SelectedInstance?.ProcessId != null && !IsLoading;
    }

    /// <summary>
    /// 跳转到指定配置段
    /// </summary>
    private void JumpToSection(ConfigSection section)
    {
        if (section?.StartLineNumber > 0)
        {
            JumpToLineRequested?.Invoke(section.StartLineNumber);
        }
    }

    /// <summary>
    /// 添加配置段
    /// </summary>
    private async Task AddSectionAsync()
    {
        if (SelectedTemplate == null) return;

        try
        {
            var template = ConfigSectionTemplateProvider.GetTemplate(SelectedTemplate.SectionType);
            if (template == null) return;

            // 验证是否可以在当前位置添加此类型的配置段
            if (!CanAddSectionAtCurrentPosition(template.SectionType))
            {
                System.Windows.MessageBox.Show(
                    $"无法在当前位置添加 {template.Name} 配置段。\n" +
                    $"请选择合适的父节点或检查nginx配置结构规则。",
                    "结构错误",
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            string? sectionName = null;
            
            // 如果模板需要名称，提示用户输入
             if (template.RequiresName)
             {
                 sectionName = Microsoft.VisualBasic.Interaction.InputBox(
                     $"请输入 {template.Name} 的名称:",
                     "配置段名称",
                     template.DefaultName ?? "");
                 
                 if (string.IsNullOrWhiteSpace(sectionName))
                 {
                     System.Windows.MessageBox.Show("配置段名称不能为空", "错误",
                         System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                     return;
                 }
             }
             else
             {
                 sectionName = template.DefaultName ?? "";
             }

            // 创建新的配置段
            var newSection = new ConfigSection
            {
                Id = GenerateNewSectionId(),
                ConfigFileId = CurrentConfig?.Id ?? 0,
                SectionType = template.SectionType.ToString().ToLower(),
                Name = sectionName,
                Content = ExtractSectionContent(template.Template),
                ParentId = SelectedSection?.Id,
                StartLineNumber = 0,
                EndLineNumber = 0
            };

            // 智能插入到正确位置
            InsertSectionAtOptimalPosition(newSection, template.SectionType);

            // 重新生成配置内容
            await RegenerateConfigContentAsync();
            
            // 选中新添加的段
            SelectedSection = newSection;
            newSection.IsExpanded = true;
            
            HasUnsavedChanges = true;
            
            System.Windows.MessageBox.Show($"已添加 {template.Name} 配置段", "成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"添加配置段失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 删除配置段
    /// </summary>
    private async Task DeleteSectionAsync()
    {
        if (SelectedSection == null) return;

        // 验证删除操作的结构合法性
        var validationResult = ValidateDeleteOperation(SelectedSection);
        if (!validationResult.IsValid)
        {
            System.Windows.MessageBox.Show(
                $"无法删除此配置段：\n{validationResult.ErrorMessage}",
                "结构错误",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var sectionName = !string.IsNullOrEmpty(SelectedSection.Name) ? SelectedSection.Name : SelectedSection.SectionType;
        var hasChildren = SelectedSection.Children?.Any() == true;
        
        var message = hasChildren 
            ? $"确定要删除配置段 '{sectionName}' 及其所有子段吗？" 
            : $"确定要删除配置段 '{sectionName}' 吗？";

        var result = System.Windows.MessageBox.Show(
            message,
            "确认删除",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            var sectionToDelete = SelectedSection;
            
            // 从父段或根集合中移除
            if (sectionToDelete.Parent != null)
            {
                sectionToDelete.Parent.RemoveChild(sectionToDelete);
            }
            else
            {
                ConfigSections.Remove(sectionToDelete);
            }

            // 递归删除所有子段
            await DeleteSectionRecursiveAsync(sectionToDelete);
            
            // 重新生成配置内容
            await RegenerateConfigContentAsync();
            
            // 清除选择
            SelectedSection = null;
            
            HasUnsavedChanges = true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"删除配置段失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 上移配置段
    /// </summary>
    private void MoveSectionUp()
    {
        if (SelectedSection == null) return;
        
        try
        {
            var parent = SelectedSection.Parent;
            var siblings = parent?.Children ?? ConfigSections;
            var currentIndex = siblings.IndexOf(SelectedSection);
            
            if (currentIndex > 0)
            {
                // 在同级中上移
                if (parent != null)
                {
                    parent.MoveChild(currentIndex, currentIndex - 1);
                }
                else
                {
                    ConfigSections.Move(currentIndex, currentIndex - 1);
                }
                
                // 重新生成配置内容
                _ = RegenerateConfigContentAsync();
                HasUnsavedChanges = true;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"移动配置段失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 下移配置段
    /// </summary>
    private void MoveSectionDown()
    {
        if (SelectedSection == null) return;
        
        try
        {
            var parent = SelectedSection.Parent;
            var siblings = parent?.Children ?? ConfigSections;
            var currentIndex = siblings.IndexOf(SelectedSection);
            
            if (currentIndex < siblings.Count - 1)
            {
                // 在同级中下移
                if (parent != null)
                {
                    parent.MoveChild(currentIndex, currentIndex + 1);
                }
                else
                {
                    ConfigSections.Move(currentIndex, currentIndex + 1);
                }
                
                // 重新生成配置内容
                _ = RegenerateConfigContentAsync();
                HasUnsavedChanges = true;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"移动配置段失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 展开所有配置段
    /// </summary>
    private void ExpandAll()
    {
        foreach (var section in ConfigSections)
        {
            section.IsExpanded = true;
        }
    }

    /// <summary>
    /// 折叠所有配置段
    /// </summary>
    private void CollapseAll()
    {
        foreach (var section in ConfigSections)
        {
            section.IsExpanded = false;
        }
    }

    /// <summary>
    /// 刷新配置段列表
    /// </summary>
    private async Task RefreshConfigSectionsAsync()
    {
        if (CurrentConfig == null) return;

        try
        {
            CurrentConfig.Content = ConfigContent;
            var updatedConfig = await _configService.ParseConfigAsync(CurrentConfig.Content);
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ConfigSections.Clear();
                foreach (var section in updatedConfig.Sections)
                {
                    ConfigSections.Add(section);
                }
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"刷新配置段失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 检查是否可以添加配置段
    /// </summary>
    private bool CanAddSection()
    {
        return SelectedTemplate != null && CurrentConfig != null && !IsLoading;
    }

    /// <summary>
    /// 检查是否可以删除配置段
    /// </summary>
    private bool CanDeleteSection()
    {
        return SelectedSection != null && !IsLoading;
    }

    /// <summary>
     /// 检查是否可以上移配置段
     /// </summary>
     private bool CanMoveSectionUp()
     {
         if (SelectedSection == null) return false;
         
         var parent = SelectedSection.Parent;
         var siblings = parent?.Children ?? ConfigSections;
         return siblings.IndexOf(SelectedSection) > 0;
     }

     /// <summary>
     /// 检查是否可以下移配置段
     /// </summary>
     private bool CanMoveSectionDown()
     {
         if (SelectedSection == null) return false;
         
         var parent = SelectedSection.Parent;
         var siblings = parent?.Children ?? ConfigSections;
         return siblings.IndexOf(SelectedSection) < siblings.Count - 1;
     }

     /// <summary>
     /// 生成新的配置段ID
     /// </summary>
     private int GenerateNewSectionId()
     {
         var maxId = 0;
         foreach (var section in ConfigSections)
         {
             maxId = Math.Max(maxId, GetMaxIdRecursive(section));
         }
         return maxId + 1;
     }

     /// <summary>
     /// 递归获取最大ID
     /// </summary>
     private int GetMaxIdRecursive(ConfigSection section)
     {
         var maxId = section.Id;
         if (section.Children != null)
         {
             foreach (var child in section.Children)
             {
                 maxId = Math.Max(maxId, GetMaxIdRecursive(child));
             }
         }
         return maxId;
     }

     /// <summary>
     /// 提取配置段内容
     /// </summary>
     private string ExtractSectionContent(string template)
     {
         // 移除模板中的注释和空行，保留核心配置内容
         var lines = template.Split('\n')
             .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
             .ToArray();
         return string.Join("\n", lines);
     }

     /// <summary>
     /// 递归删除配置段
     /// </summary>
     private async Task DeleteSectionRecursiveAsync(ConfigSection section)
     {
         if (section.Children != null)
         {
             var childrenToDelete = section.Children.ToList();
             foreach (var child in childrenToDelete)
             {
                 await DeleteSectionRecursiveAsync(child);
             }
         }
         
         // 从数据库中删除（如果有ID）
         if (section.Id > 0 && CurrentConfig?.Id > 0)
         {
             await _repository.DeleteConfigSectionAsync(section.Id);
         }
     }

     /// <summary>
     /// 重新生成配置内容
     /// </summary>
     private async Task RegenerateConfigContentAsync()
     {
         try
         {
             var content = await _configService.GenerateConfigContentAsync(ConfigSections.ToList());
             ConfigContent = content;
             
             // 更新行号
             await UpdateLineNumbersAsync();
         }
         catch (Exception ex)
         {
             System.Windows.MessageBox.Show($"重新生成配置内容失败: {ex.Message}", "错误",
                 System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
         }
     }

     /// <summary>
     /// 更新配置段行号
     /// </summary>
     private async Task UpdateLineNumbersAsync()
     {
         await Task.Run(() =>
         {
             var lines = ConfigContent.Split('\n');
             int currentLine = 1;
             
             foreach (var section in ConfigSections)
             {
                 currentLine = UpdateSectionLineNumbersRecursive(section, lines, currentLine);
             }
         });
     }

     /// <summary>
     /// 递归更新配置段行号
     /// </summary>
     private int UpdateSectionLineNumbersRecursive(ConfigSection section, string[] lines, int currentLine)
     {
         section.StartLineNumber = currentLine;
         
         // 如果不是主段，需要计算段标题行
         if (section.SectionType != ConfigSectionTypes.Main)
         {
             currentLine++; // 段标题行 (例如: "server {")
         }
         
         // 计算段内容的行数
         if (!string.IsNullOrEmpty(section.Content))
         {
             var contentLines = section.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
             currentLine += contentLines.Length;
         }
         
         // 递归处理子段
         if (section.Children != null)
         {
             foreach (var child in section.Children)
             {
                 currentLine = UpdateSectionLineNumbersRecursive(child, lines, currentLine);
             }
         }
         
         // 如果不是主段，需要计算结束大括号行
         if (section.SectionType != ConfigSectionTypes.Main)
         {
             currentLine++; // 结束大括号行 (例如: "}")
         }
         
         section.EndLineNumber = currentLine - 1;
         
         return currentLine;
     }

     /// <summary>
     /// 加载可用的配置段模板
     /// </summary>
     private void LoadAvailableTemplates()
     {
         AvailableTemplates.Clear();
         
         // 如果有选中的配置段，只显示可以作为其子段的模板
         if (SelectedSection != null)
         {
             var availableChildTemplates = ConfigSectionTemplateProvider.GetAvailableChildTemplates(SelectedSection.SectionTypeEnum);
             foreach (var template in availableChildTemplates)
             {
                 AvailableTemplates.Add(template);
             }
         }
         else
         {
             // 如果没有选中配置段，显示可以作为根段的模板
             var rootTemplates = ConfigSectionTemplateProvider.GetAllTemplates()
                 .Where(t => t.AllowedParents.Contains(NginxSectionType.Main) || !t.AllowedParents.Any());
             foreach (var template in rootTemplates)
             {
                 AvailableTemplates.Add(template);
             }
         }
         
         if (AvailableTemplates.Any())
         {
             SelectedTemplate = AvailableTemplates.First();
         }
     }

       /// <summary>
      /// 手动刷新上下文模板（调试用）
      /// </summary>
      public void RefreshContextualTemplates()
      {
          System.Diagnostics.Debug.WriteLine("手动刷新上下文模板被调用");
          UpdateContextualTemplates();
      }

      /// <summary>
      /// 获取调试信息
      /// </summary>
      public string GetDebugInfo()
      {
          var info = new System.Text.StringBuilder();
          info.AppendLine($"SelectedSection: {SelectedSection?.SectionType ?? "null"}");
          info.AppendLine($"SelectedSection.SectionTypeEnum: {SelectedSection?.SectionTypeEnum}");
          info.AppendLine($"ContextualTemplates.Count: {ContextualTemplates.Count}");
          info.AppendLine($"AvailableTemplates.Count: {AvailableTemplates.Count}");
          
          if (SelectedSection != null)
          {
              var directTemplates = ConfigSectionTemplateProvider.GetAvailableChildTemplates(SelectedSection.SectionTypeEnum);
              info.AppendLine($"直接调用GetAvailableChildTemplates返回: {directTemplates.Count()} 个模板");
              foreach (var template in directTemplates)
              {
                  info.AppendLine($"  - {template.Name} ({template.SectionType})");
              }
          }
          
          return info.ToString();
      }

      /// <summary>
      /// 更新基于上下文的模板列表
      /// </summary>
      private void UpdateContextualTemplates()
      {
          System.Windows.Application.Current.Dispatcher.Invoke(() =>
          {
              ContextualTemplates.Clear();
              
              if (SelectedSection != null)
              {
                  // 获取当前选中节点可以添加的子模板
                  var childTemplates = ConfigSectionTemplateProvider.GetAvailableChildTemplates(SelectedSection.SectionTypeEnum);
                  foreach (var template in childTemplates)
                  {
                      ContextualTemplates.Add(template);
                  }
                  
                  // 调试输出
              System.Diagnostics.Debug.WriteLine($"UpdateContextualTemplates: 选中节点 {SelectedSection.SectionType}, 可用模板数量: {ContextualTemplates.Count}");
              
              // 添加更详细的调试信息
              System.Diagnostics.Debug.WriteLine($"详细信息: SectionTypeEnum={SelectedSection.SectionTypeEnum}, DisplayName={SelectedSection.DisplayName}");
              
              // 调试父模板信息
              var parentTemplate = ConfigSectionTemplateProvider.GetTemplate(SelectedSection.SectionTypeEnum);
              if (parentTemplate != null)
              {
                  System.Diagnostics.Debug.WriteLine($"父模板: {parentTemplate.Name}, 允许的子类型数量: {parentTemplate.AllowedChildren.Count}");
                  foreach (var allowedChild in parentTemplate.AllowedChildren)
                  {
                      System.Diagnostics.Debug.WriteLine($"  允许的子类型: {allowedChild}");
                  }
              }
              else
              {
                  System.Diagnostics.Debug.WriteLine("未找到父模板!");
              }
              
              foreach (var template in ContextualTemplates)
              {
                  System.Diagnostics.Debug.WriteLine($"  - {template.Name} ({template.SectionType})");
              }
              }
              else
              {
                  // 如果没有选中节点，显示根级别的模板
                  var rootTemplates = ConfigSectionTemplateProvider.GetAllTemplates()
                      .Where(t => t.AllowedParents.Contains(NginxSectionType.Main) || !t.AllowedParents.Any());
                  foreach (var template in rootTemplates)
                  {
                      ContextualTemplates.Add(template);
                  }
                  
                  // 调试输出
                  System.Diagnostics.Debug.WriteLine($"UpdateContextualTemplates: 无选中节点, 根级模板数量: {ContextualTemplates.Count}");
              }
              
              // 自动选择第一个模板
              if (ContextualTemplates.Any())
              {
                  SelectedTemplate = ContextualTemplates.First();
                  System.Diagnostics.Debug.WriteLine($"自动选择第一个模板: {SelectedTemplate.Name}");
              }
              else
              {
                  SelectedTemplate = null;
                  System.Diagnostics.Debug.WriteLine("没有可用模板，SelectedTemplate设为null");
              }
              
              // 强制触发属性更改通知
              OnPropertyChanged(nameof(ContextualTemplates));
          });
      }

      /// <summary>
      /// 验证是否可以在当前位置添加指定类型的配置段
      /// </summary>
      private bool CanAddSectionAtCurrentPosition(NginxSectionType sectionType)
      {
          if (SelectedSection != null)
          {
              // 检查选中的节点是否可以添加此类型的子段
              return SelectedSection.CanAddChild(sectionType);
          }
          else
          {
              // 检查是否可以作为根段添加
              var template = ConfigSectionTemplateProvider.GetTemplate(sectionType);
              return template?.AllowedParents.Contains(NginxSectionType.Main) == true || 
                     template?.AllowedParents.Any() == false;
          }
      }

      /// <summary>
       /// 智能插入配置段到最佳位置
       /// </summary>
       private void InsertSectionAtOptimalPosition(ConfigSection newSection, NginxSectionType sectionType)
       {
           if (SelectedSection != null && SelectedSection.CanAddChild(sectionType))
           {
               // 作为选中节点的子段添加
               newSection.Parent = SelectedSection;
               newSection.ParentId = SelectedSection.Id;
               SelectedSection.AddChild(newSection);
           }
           else if (SelectedSection?.Parent != null && SelectedSection.Parent.CanAddChild(sectionType))
           {
               // 作为选中节点的兄弟节点添加（同级插入）
               newSection.Parent = SelectedSection.Parent;
               newSection.ParentId = SelectedSection.Parent.Id;
               
               // 找到选中节点在父节点中的位置，在其后插入
               var siblings = SelectedSection.Parent.Children;
               var selectedIndex = siblings.IndexOf(SelectedSection);
               if (selectedIndex >= 0 && selectedIndex < siblings.Count - 1)
               {
                   siblings.Insert(selectedIndex + 1, newSection);
               }
               else
               {
                   siblings.Add(newSection);
               }
           }
           else
           {
               // 作为根段添加
               newSection.Parent = null;
               newSection.ParentId = null;
               ConfigSections.Add(newSection);
           }
       }

       /// <summary>
       /// 验证删除操作的结构合法性
       /// </summary>
       private ValidationResult ValidateDeleteOperation(ConfigSection sectionToDelete)
       {
           // 检查是否为必需的配置段
           if (IsRequiredSection(sectionToDelete))
           {
               return new ValidationResult
               {
                   IsValid = false,
                   ErrorMessage = $"{sectionToDelete.SectionType} 是nginx配置的必需段，不能删除。"
               };
           }

           // 检查删除后是否会导致孤立的子段
           if (WouldCreateOrphanedSections(sectionToDelete))
           {
               return new ValidationResult
               {
                   IsValid = false,
                   ErrorMessage = "删除此配置段会导致其子段失去有效的父段，违反nginx配置结构规则。"
               };
           }

           return new ValidationResult { IsValid = true };
       }

       /// <summary>
       /// 检查是否为必需的配置段
       /// </summary>
       private bool IsRequiredSection(ConfigSection section)
       {
           // main段是必需的（虽然通常不会被删除，因为它是根段）
           if (section.SectionTypeEnum == NginxSectionType.Main)
               return true;

           // 如果是唯一的events段，则不能删除
           if (section.SectionTypeEnum == NginxSectionType.Events)
           {
               var allEventsSections = GetAllSectionsOfType(NginxSectionType.Events);
               return allEventsSections.Count() <= 1;
           }

           return false;
       }

       /// <summary>
       /// 检查删除后是否会创建孤立的配置段
       /// </summary>
       private bool WouldCreateOrphanedSections(ConfigSection sectionToDelete)
       {
           if (sectionToDelete.Children == null || !sectionToDelete.Children.Any())
               return false;

           // 检查每个子段是否能找到其他合适的父段
           foreach (var child in sectionToDelete.Children)
           {
               if (!CanFindAlternativeParent(child, sectionToDelete))
               {
                   return true;
               }
           }

           return false;
       }

       /// <summary>
       /// 检查配置段是否能找到替代的父段
       /// </summary>
       private bool CanFindAlternativeParent(ConfigSection child, ConfigSection excludeParent)
       {
           var childTemplate = ConfigSectionTemplateProvider.GetTemplate(child.SectionTypeEnum);
           if (childTemplate == null) return false;

           // 检查是否可以成为根段
           if (childTemplate.AllowedParents.Contains(NginxSectionType.Main))
               return true;

           // 检查是否有其他合适的父段
           var allSections = GetAllSections().Where(s => s != excludeParent && s != child);
           return allSections.Any(s => s.CanAddChild(child.SectionTypeEnum));
       }

       /// <summary>
       /// 获取指定类型的所有配置段
       /// </summary>
       private IEnumerable<ConfigSection> GetAllSectionsOfType(NginxSectionType sectionType)
       {
           return GetAllSections().Where(s => s.SectionTypeEnum == sectionType);
       }

       /// <summary>
       /// 获取所有配置段（递归）
       /// </summary>
       private IEnumerable<ConfigSection> GetAllSections()
       {
           var allSections = new List<ConfigSection>();
           
           foreach (var rootSection in ConfigSections)
           {
               allSections.Add(rootSection);
               allSections.AddRange(GetAllDescendants(rootSection));
           }
           
           return allSections;
       }

       /// <summary>
       /// 获取配置段的所有后代
       /// </summary>
       private IEnumerable<ConfigSection> GetAllDescendants(ConfigSection section)
       {
           var descendants = new List<ConfigSection>();
           
           if (section.Children != null)
           {
               foreach (var child in section.Children)
               {
                   descendants.Add(child);
                   descendants.AddRange(GetAllDescendants(child));
               }
           }
           
           return descendants;
       }

       /// <summary>
       /// 将平面的配置段列表构建为树形结构
       /// </summary>
       private List<ConfigSection> BuildSectionTree(List<ConfigSection> flatSections)
       {
           var sectionDict = flatSections.ToDictionary(s => s.Id, s => s);
           var rootSections = new List<ConfigSection>();

           foreach (var section in flatSections)
           {
               // 清空现有的子节点集合
               section.Children.Clear();
               
               if (section.ParentId.HasValue && sectionDict.ContainsKey(section.ParentId.Value))
               {
                   // 有父节点，添加到父节点的子集合中
                   var parent = sectionDict[section.ParentId.Value];
                   parent.Children.Add(section);
                   section.Parent = parent;
               }
               else
               {
                   // 没有父节点，是根节点
                   rootSections.Add(section);
               }
           }

           return rootSections;
       }
   }

/// <summary>
/// 验证结果类
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}