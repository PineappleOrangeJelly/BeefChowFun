using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.DalamudServices;
using BeefChowFun.Workers;
using BeefChowFun.Windows;
using System;
using System.Collections.Generic;

namespace BeefChowFun;

public sealed class BeefChowFun : IDalamudPlugin
{
    public string Name => "BeefChowFun";
    internal static BeefChowFun Instance { get; private set; } = null!;

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IMarketBoard MarketBoard { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
    [PluginService] internal static IToastGui ToastGui { get; private set; } = null!;

    public Configuration Configuration { get; private set; }
    public readonly WindowSystem WindowSystem = new("BeefChowFun");
    private ConfigWindow ConfigWindow { get; init; }
    private OverlayWindow OverlayWindow { get; init; }

    private readonly List<WorkerBase> _workers = [];
    internal IReadOnlyList<WorkerBase> Workers => _workers;

    private const string CommandName = "/bcf";

    public BeefChowFun(IDalamudPluginInterface pluginInterface)
    {
        try
        {
            // 注入 Dalamud 服务（通过 [PluginService] 特性自动完成）
            Instance = this;

            PluginLog.Information("BeefChowFun 开始初始化...");

            try
            {
                PluginLog.Information("正在初始化 ECommons...");
                ECommonsMain.Init(pluginInterface, this);
                PluginLog.Information("ECommons 已成功初始化");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"ECommons 初始化失败: {ex.GetType().Name}");
                PluginLog.Error($"消息: {ex.Message}");
                PluginLog.Error($"堆栈: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    PluginLog.Error($"内部异常: {ex.InnerException.GetType().Name}");
                    PluginLog.Error($"内部消息: {ex.InnerException.Message}");
                }
                throw;
            }

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            ConfigWindow = new ConfigWindow(this);
            OverlayWindow = new OverlayWindow(this);
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(OverlayWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "打开干炒牛河配置窗口 | /bcf config"
            });

            PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
            PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUi;

            // 延迟初始化 Workers，确保 Dalamud 服务都已准备好
            Framework.RunOnFrameworkThread(() =>
            {
                try
                {
                    InitializeWorkers();
                    PluginLog.Information("Workers 已初始化");
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "Workers 延迟初始化失败");
                }
            });

            PluginLog.Information("BeefChowFun 插件已加载喵~");
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "BeefChowFun 插件初始化失败");
            throw;
        }
    }

    private void InitializeWorkers()
    {
        try
        {
            _workers.Add(new CollectWorker());
            _workers.Add(new EntrustDupsWorker());
            _workers.Add(new GilsWithdrawWorker());
            _workers.Add(new GilsShareWorker());
            _workers.Add(new RefreshWorker());
            _workers.Add(new TownDispatchWorker());
            _workers.Add(new PriceAdjustWorker());

            foreach (var worker in _workers)
            {
                try { worker.Init(); }
                catch (Exception ex) { PluginLog.Error(ex, $"Worker 初始化失败 [{worker.Name}]"); }
            }
        }
        catch (Exception ex) { PluginLog.Error(ex, "Workers 初始化失败"); }
    }

    public void Dispose()
    {
        try
        {
            PluginLog.Information("BeefChowFun 开始卸载...");

            // 先卸载 Workers
            foreach (var worker in _workers)
            {
                try 
                { 
                    worker.Uninit();
                    PluginLog.Information($"Worker 已卸载: {worker.Name}");
                }
                catch (Exception ex) 
                { 
                    PluginLog.Error(ex, $"Worker 卸载失败 [{worker.Name}]"); 
                }
            }
            _workers.Clear();

            // 取消注册事件
            try
            {
                PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
                PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
                PluginInterface.UiBuilder.OpenMainUi -= ToggleConfigUi;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "取消事件注册失败");
            }

            // 清理窗口
            try
            {
                WindowSystem.RemoveAllWindows();
                ConfigWindow.Dispose();
                OverlayWindow.Dispose();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "窗口清理失败");
            }

            // 移除命令
            try
            {
                CommandManager.RemoveHandler(CommandName);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "移除命令失败");
            }

            // 保存配置
            try
            {
                SaveConfig();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "保存配置失败");
            }

            // 最后清理 ECommons
            try
            {
                ECommonsMain.Dispose();
                PluginLog.Information("ECommons 已清理");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ECommons 清理失败");
            }

            PluginLog.Information("BeefChowFun 插件已卸载喵~");
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "BeefChowFun 插件卸载时发生严重错误");
        }
    }

    private void OnCommand(string command, string args)
    {
        var trimmed = args.Trim().ToLowerInvariant();
        if (trimmed == "help")
            Svc.Chat.Print("[BeefChowFun] 使用 /bcf 打开配置窗口");
        else
            ToggleConfigUi();
    }

    internal void SaveConfig()
    {
        try { PluginInterface.SavePluginConfig(Configuration); }
        catch (Exception ex) { PluginLog.Error(ex, "保存配置失败"); }
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    internal bool IsAnyWorkerBusy => _workers.Exists(w => w.IsBusy);
}
