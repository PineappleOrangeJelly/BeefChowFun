using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Numerics;
using Callback = ECommons.Automation.Callback;

namespace BeefChowFun.Workers;

/// <summary>
/// 刷新市场 Worker
/// 自动刷新市场板搜索结果
/// </summary>
internal unsafe class RefreshWorker : WorkerBase
{
    public override string Name => Loc.RefreshWorker;
    private TaskManager? _taskManager;
    private bool _isBusy;
    public override bool IsBusy => _isBusy;

    public override void Init()
    {
        _taskManager = new TaskManager(new(abortOnTimeout: true, timeLimitMS: 15000, showDebug: false));
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "ItemSearchResult", OnItemSearchResultSetup);
    }

    public override void Uninit()
    {
        _taskManager?.Abort();
        _taskManager = null;
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "ItemSearchResult", OnItemSearchResultSetup);
    }

    private void OnItemSearchResultSetup(AddonEvent type, AddonArgs args)
    {
        if (!Config.RefreshWorkerEnabled) return;
        // 自动刷新备用：当市场搜索结果窗口打开时自动触发
    }

    public override void DrawConfig()
    {
        ImGuiHelper.Text(Loc.RefreshWorkerDesc);
        ImGui.Spacing();
        var enabled = Config.RefreshWorkerEnabled;
        if (ImGui.Checkbox($"{Loc.Enabled}##Refresh", ref enabled))
        {
            Config.RefreshWorkerEnabled = enabled;
            BeefChowFun.Instance.SaveConfig();
        }
        if (!Config.RefreshWorkerEnabled) return;
        ImGui.Spacing();
        if (ImGui.Button($"{Loc.RefreshMarket}##RefreshStart")) StartRefresh();
        if (_isBusy) { ImGui.SameLine(); ImGui.TextColored(new Vector4(1,1,0,1), Loc.Refreshing); }
    }

    public override void DrawOverlay(string addonName)
    {
        if (addonName != "ItemSearchResult" && addonName != "RetainerList") return;
        if (ImGui.Button($"{Loc.RefreshMarket}##RefreshOverlay")) StartRefresh();
        if (_isBusy) { ImGui.SameLine(); ImGuiHelper.Text(Loc.Refreshing); }
    }

    public override bool ShouldDrawOverlay(string addonName)
        => (addonName == "ItemSearchResult" || addonName == "RetainerList") && Config.RefreshWorkerEnabled;

    private void StartRefresh()
    {
        if (_isBusy || _taskManager == null) return;
        _isBusy = true;
        Log("开始刷新市场");

        _taskManager.Enqueue(() =>
        {
            if (!EzThrottler.Throttle("RefreshMarket", 1000)) return false;
            var addon = Svc.GameGui.GetAddonByName("ItemSearchResult");
            if (addon.Address == IntPtr.Zero) { _isBusy = false; return true; }
            var unitBase = (AtkUnitBase*)(nint)addon;
            if (!unitBase->IsVisible) { _isBusy = false; return true; }
            // 调用刷新按鈕 (callback 0 = 刷新)
            Callback.Fire(unitBase, true, 0);
            return true;
        });
        _taskManager.Enqueue(() => { _isBusy = false; Log("市场已刷新"); return true; });
    }
}
