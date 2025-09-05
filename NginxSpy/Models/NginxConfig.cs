using System.ComponentModel;
using System.Collections.ObjectModel;

namespace NginxSpy.Models;

/// <summary>
/// Nginx配置文件模型
/// </summary>
public class NginxConfig
{
    /// <summary>
    /// 配置文件ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的Nginx实例ID
    /// </summary>
    public int InstanceId { get; set; }

    /// <summary>
    /// 配置文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 配置文件内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.Now;

    /// <summary>
    /// 配置是否有效
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// 配置段列表
    /// </summary>
    public List<ConfigSection> Sections { get; set; } = new();

    /// <summary>
    /// 关联的Nginx实例
    /// </summary>
    public NginxInstance? Instance { get; set; }
}

/// <summary>
/// 配置段模型 - 支持树状结构
/// </summary>
public class ConfigSection : INotifyPropertyChanged
{
    private bool _isExpanded = true;
    private bool _isSelected;
    private string _displayName = string.Empty;
    private ObservableCollection<ConfigSection> _children = new();

    /// <summary>
    /// 配置段ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的配置文件ID
    /// </summary>
    public int ConfigFileId { get; set; }

    /// <summary>
    /// 配置段类型
    /// </summary>
    public string SectionType { get; set; } = string.Empty;

    /// <summary>
    /// 配置段名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 配置段内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 父配置段ID
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// 起始行号
    /// </summary>
    public int StartLineNumber { get; set; }

    /// <summary>
    /// 结束行号
    /// </summary>
    public int EndLineNumber { get; set; }

    /// <summary>
    /// 子配置段列表 - 支持数据绑定
    /// </summary>
    public ObservableCollection<ConfigSection> Children
    {
        get => _children;
        set
        {
            _children = value;
            OnPropertyChanged(nameof(Children));
            OnPropertyChanged(nameof(HasChildren));
        }
    }

    /// <summary>
    /// 父配置段
    /// </summary>
    public ConfigSection? Parent { get; set; }

    /// <summary>
    /// 关联的配置文件
    /// </summary>
    public NginxConfig? ConfigFile { get; set; }

    /// <summary>
    /// 树节点是否展开
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            OnPropertyChanged(nameof(IsExpanded));
        }
    }

    /// <summary>
    /// 树节点是否被选中
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(_displayName))
                return _displayName;

            return string.IsNullOrEmpty(Name) ? SectionType : $"{SectionType} {Name}";
        }
        set
        {
            _displayName = value;
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    /// <summary>
    /// 是否有子节点
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// 配置段深度级别
    /// </summary>
    public int Level
    {
        get
        {
            int level = 0;
            var parent = Parent;
            while (parent != null)
            {
                level++;
                parent = parent.Parent;
            }
            return level;
        }
    }

    /// <summary>
    /// 获取配置段类型的枚举值
    /// </summary>
    public NginxSectionType SectionTypeEnum
    {
        get
        {
            return SectionType.ToLower() switch
            {
                "main" => NginxSectionType.Main,
                "events" => NginxSectionType.Events,
                "http" => NginxSectionType.Http,
                "server" => NginxSectionType.Server,
                "location" => NginxSectionType.Location,
                "upstream" => NginxSectionType.Upstream,
                "if" => NginxSectionType.If,
                "map" => NginxSectionType.Map,
                "geo" => NginxSectionType.Geo,
                "limit" => NginxSectionType.Limit,
                "types" => NginxSectionType.Types,
                "split_clients" => NginxSectionType.SplitClients,
                "stream" => NginxSectionType.Stream,
                "mail" => NginxSectionType.Mail,
                _ => NginxSectionType.Main
            };
        }
    }

    /// <summary>
    /// 添加子配置段
    /// </summary>
    public void AddChild(ConfigSection child)
    {
        child.Parent = this;
        child.ParentId = this.Id;
        Children.Add(child);
        OnPropertyChanged(nameof(HasChildren));
    }

    /// <summary>
    /// 移除子配置段
    /// </summary>
    public bool RemoveChild(ConfigSection child)
    {
        var removed = Children.Remove(child);
        if (removed)
        {
            child.Parent = null;
            child.ParentId = null;
            OnPropertyChanged(nameof(HasChildren));
        }
        return removed;
    }

    /// <summary>
    /// 移动子配置段位置
    /// </summary>
    public void MoveChild(int oldIndex, int newIndex)
    {
        if (oldIndex >= 0 && oldIndex < Children.Count && 
            newIndex >= 0 && newIndex < Children.Count && 
            oldIndex != newIndex)
        {
            Children.Move(oldIndex, newIndex);
        }
    }

    /// <summary>
    /// 获取所有后代配置段
    /// </summary>
    public IEnumerable<ConfigSection> GetAllDescendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.GetAllDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// 获取根配置段
    /// </summary>
    public ConfigSection GetRoot()
    {
        var current = this;
        while (current.Parent != null)
        {
            current = current.Parent;
        }
        return current;
    }

    /// <summary>
    /// 检查是否可以添加指定类型的子配置段
    /// </summary>
    public bool CanAddChild(NginxSectionType childType)
    {
        return ConfigSectionTemplateProvider.CanAddChild(SectionTypeEnum, childType);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 配置段类型常量
/// </summary>
public static class ConfigSectionTypes
{
    public const string Main = "main";
    public const string Events = "events";
    public const string Http = "http";
    public const string Server = "server";
    public const string Location = "location";
    public const string Upstream = "upstream";
    public const string Stream = "stream";
}

/// <summary>
/// 配置指令模型
/// </summary>
public class ConfigDirective
{
    /// <summary>
    /// 指令名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 指令参数
    /// </summary>
    public List<string> Parameters { get; set; } = new();

    /// <summary>
    /// 指令注释
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// 行号
    /// </summary>
    public int LineNumber { get; set; }
}