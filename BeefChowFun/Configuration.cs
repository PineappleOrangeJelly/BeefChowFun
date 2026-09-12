using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace BeefChowFun;

/// <summary>
/// 插件配置类
/// </summary>
[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    
    // ===== 全局设置 =====
    
    /// <summary>
    /// 是否启用插件
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 是否显示配置窗口
    /// </summary>
    public bool ShowConfigWindow { get; set; } = false;
    
    /// <summary>
    /// 是否显示悬浮窗
    /// </summary>
    public bool ShowOverlay { get; set; } = true;
    
    // ===== CollectWorker 设置 =====
    
    public bool CollectWorkerEnabled { get; set; } = false;
    
    // ===== EntrustDupsWorker 设置 =====
    
    public bool EntrustDupsWorkerEnabled { get; set; } = false;
    
    // ===== GilsWithdrawWorker 设置 =====
    
    public bool GilsWithdrawWorkerEnabled { get; set; } = false;
    
    /// <summary>
    /// 提取金币的目标金额
    /// </summary>
    public int WithdrawGilAmount { get; set; } = 0;
    
    // ===== GilsShareWorker 设置 =====
    
    public bool GilsShareWorkerEnabled { get; set; } = false;
    
    /// <summary>
    /// 金币分配目标金额
    /// </summary>
    public int ShareGilAmount { get; set; } = 0;
    
    // ===== RefreshWorker 设置 =====
    
    public bool RefreshWorkerEnabled { get; set; } = false;
    
    // ===== TownDispatchWorker 设置 =====
    
    public bool TownDispatchWorkerEnabled { get; set; } = false;
    
    // ===== PriceAdjustWorker 设置 =====
    
    public bool PriceAdjustWorkerEnabled { get; set; } = false;
    
    /// <summary>
    /// 改价策略：0=最低价, 1=降价, 2=固定价格
    /// </summary>
    public int PriceAdjustStrategy { get; set; } = 0;
    
    /// <summary>
    /// 降价金额（策略1使用）
    /// </summary>
    public int PriceReductionAmount { get; set; } = 1;
    
    /// <summary>
    /// 固定价格（策略2使用）
    /// </summary>
    public int FixedPrice { get; set; } = 0;
    
    /// <summary>
    /// 最低价格限制
    /// </summary>
    public int MinimumPrice { get; set; } = 1;
    
    /// <summary>
    /// 最高价格限制
    /// </summary>
    public int MaximumPrice { get; set; } = 999999999;
    
    // ===== 高级设置 =====
    
    /// <summary>
    /// 操作延迟（毫秒）
    /// </summary>
    public int OperationDelay { get; set; } = 500;
    
    /// <summary>
    /// 是否启用调试日志
    /// </summary>
    public bool EnableDebugLog { get; set; } = false;
}
