using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NginxSpy.Infrastructure;

/// <summary>
/// ViewModel基类，实现INotifyPropertyChanged接口
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 触发属性变更通知
    /// </summary>
    /// <param name="propertyName">属性名称，自动获取调用者名称</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 设置属性值并触发变更通知
    /// </summary>
    /// <typeparam name="T">属性类型</typeparam>
    /// <param name="field">字段引用</param>
    /// <param name="value">新值</param>
    /// <param name="propertyName">属性名称</param>
    /// <returns>如果值发生变化返回true，否则返回false</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// 设置属性值并触发变更通知，支持自定义相等比较器
    /// </summary>
    /// <typeparam name="T">属性类型</typeparam>
    /// <param name="field">字段引用</param>
    /// <param name="value">新值</param>
    /// <param name="comparer">相等比较器</param>
    /// <param name="propertyName">属性名称</param>
    /// <returns>如果值发生变化返回true，否则返回false</returns>
    protected bool SetProperty<T>(ref T field, T value, IEqualityComparer<T> comparer, [CallerMemberName] string? propertyName = null)
    {
        if (comparer.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// 触发多个属性的变更通知
    /// </summary>
    /// <param name="propertyNames">属性名称列表</param>
    protected void OnPropertiesChanged(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}