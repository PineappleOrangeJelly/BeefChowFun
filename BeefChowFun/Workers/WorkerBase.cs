using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using System;

namespace BeefChowFun.Workers;

/// <summary>
/// Worker 基类
/// 所有 Worker 都继承自此类
/// </summary>
internal abstract class WorkerBase
{
    /// <summary>
    /// Worker 名称
    /// </summary>
    public abstract string Name { get; }
    
    /// <summary>
    /// Worker 是否正在工作
    /// </summary>
    public abstract bool IsBusy { get; }
    
    /// <summary>
    /// 配置访问
    /// </summary>
    protected Configuration Config => BeefChowFun.Instance.Configuration;
    
    /// <summary>
    /// 初始化 Worker
    /// 注册事件监听、Hook 等
    /// </summary>
    public abstract void Init();
    
    /// <summary>
    /// 卸载 Worker
    /// 清理资源、取消注册等
    /// </summary>
    public abstract void Uninit();
    
    /// <summary>
    /// 绘制配置界面
    /// 在插件配置窗口中显示
    /// </summary>
    public virtual void DrawConfig()
    {
        // 默认不绘制配置
    }
    
    /// <summary>
    /// 绘制悬浮窗
    /// 在雇员列表旁边显示
    /// </summary>
    /// <param name="addonName">当前激活的 Addon 名称</param>
    public virtual void DrawOverlay(string addonName)
    {
        // 默认不绘制悬浮窗
    }
    
    /// <summary>
    /// 是否应该绘制配置界面
    /// </summary>
    /// <returns>true 表示应该绘制</returns>
    public virtual bool ShouldDrawConfig()
    {
        return true;
    }
    
    /// <summary>
    /// 是否应该绘制悬浮窗
    /// </summary>
    /// <param name="addonName">当前激活的 Addon 名称</param>
    /// <returns>true 表示应该绘制</returns>
    public virtual bool ShouldDrawOverlay(string addonName)
    {
        return false;
    }
    
    /// <summary>
    /// 日志输出
    /// </summary>
    protected void Log(string message)
    {
        Svc.Log.Info($"[{Name}] {message}");
    }
    
    /// <summary>
    /// 错误日志输出
    /// </summary>
    protected void LogError(string message)
    {
        Svc.Log.Error($"[{Name}] {message}");
    }
    
    /// <summary>
    /// 调试日志输出
    /// </summary>
    protected void LogDebug(string message)
    {
        if (Config.EnableDebugLog)
        {
            Svc.Log.Debug($"[{Name}] {message}");
        }
    }
}
