namespace BeefChowFun;

/// <summary>
/// 本地化字符串
/// 所有界面文本都在这里定义
/// </summary>
internal static class Loc
{
    // ===== 插件通用 =====
    public const string PluginName = "干炒牛河";
    public const string PluginDescription = "雇员自动化作业工具";
    
    // ===== 配置窗口 =====
    public const string ConfigWindowTitle = "干炒牛河 - 配置";
    public const string Enabled = "启用";
    public const string Disabled = "禁用";
    public const string Settings = "设置";
    public const string Save = "保存";
    public const string Cancel = "取消";
    public const string Apply = "应用";
    
    // ===== 悬浮窗 =====
    public const string OverlayTitle = "雇员作业";
    
    // ===== Worker 名称 =====
    public const string CollectWorker = "收取雇员";
    public const string EntrustDupsWorker = "委托重复物品";
    public const string GilsWithdrawWorker = "提取金币";
    public const string GilsShareWorker = "分配金币";
    public const string RefreshWorker = "刷新市场";
    public const string TownDispatchWorker = "城镇派遣";
    public const string PriceAdjustWorker = "价格调整";
    
    // ===== CollectWorker =====
    public const string CollectWorkerDesc = "自动收取所有雇员完成的任务";
    public const string CollectAll = "收取全部";
    public const string Collecting = "收取中...";
    
    // ===== EntrustDupsWorker =====
    public const string EntrustDupsWorkerDesc = "自动将背包中的重复物品委托给雇员存储";
    public const string EntrustDups = "委托重复物品";
    public const string Entrusting = "委托中...";
    
    // ===== GilsWithdrawWorker =====
    public const string GilsWithdrawWorkerDesc = "从所有雇员处提取金币";
    public const string WithdrawGils = "提取金币";
    public const string WithdrawAmount = "提取金额";
    public const string Withdrawing = "提取中...";
    
    // ===== GilsShareWorker =====
    public const string GilsShareWorkerDesc = "在所有雇员之间平均分配金币";
    public const string ShareGils = "分配金币";
    public const string ShareAmount = "分配总额";
    public const string Sharing = "分配中...";
    
    // ===== RefreshWorker =====
    public const string RefreshWorkerDesc = "刷新市场板搜索结果";
    public const string RefreshMarket = "刷新市场";
    public const string Refreshing = "刷新中...";
    
    // ===== TownDispatchWorker =====
    public const string TownDispatchWorkerDesc = "自动派遣雇员执行城镇任务";
    public const string DispatchTown = "城镇派遣";
    public const string Dispatching = "派遣中...";
    
    // ===== PriceAdjustWorker =====
    public const string PriceAdjustWorkerDesc = "自动调整雇员出售物品的价格";
    public const string AdjustPrice = "调整价格";
    public const string PriceStrategy = "改价策略";
    public const string StrategyLowest = "最低价";
    public const string StrategyReduction = "降价";
    public const string StrategyFixed = "固定价格";
    public const string ReductionAmount = "降价金额";
    public const string FixedPrice = "固定价格";
    public const string MinPrice = "最低价格";
    public const string MaxPrice = "最高价格";
    public const string Adjusting = "调整中...";
    
    // ===== 状态消息 =====
    public const string Ready = "就绪";
    public const string Working = "工作中";
    public const string Completed = "已完成";
    public const string Failed = "失败";
    public const string Cancelled = "已取消";
    
    // ===== 错误消息 =====
    public const string ErrorRetainerListNotOpen = "请先打开雇员列表";
    public const string ErrorNoRetainers = "没有找到雇员";
    public const string ErrorBusy = "当前正在执行其他操作";
    public const string ErrorUnknown = "未知错误";
    
    // ===== 通用 =====
    public const string Start = "开始";
    public const string Stop = "停止";
    public const string Reset = "重置";
    public const string Close = "关闭";
    public const string Help = "帮助";
    public const string About = "关于";
}
