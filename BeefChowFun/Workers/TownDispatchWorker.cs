using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Numerics;
using Callback = ECommons.Automation.Callback;

namespace BeefChowFun.Workers;

/// <summary>
/// 城镇派遣 Worker
/// 自动派遣雇员执行城镇任务
/// </summary>
internal unsafe class TownDispatchWorker : WorkerBase
{
    public override string Name => Loc.TownDispatchWorker;
    private TaskManager? _taskManager;
    private bool _isBusy;
    public override bool IsBusy => _isBusy;

    public override void Init()
    {
        _taskManager = new TaskManager(new(abortOnTimeout: true, timeLimitMS: 30000, showDebug: false));
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerList", OnRetainerListSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", OnSelectStringSetup);
    }

    public override void Uninit()
    {
        _taskManager?.Abort();
        _taskManager = null;
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerList", OnRetainerListSetup);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "SelectString", OnSelectStringSetup);
    }

    private void OnRetainerListSetup(AddonEvent type, AddonArgs args) { }
    private void OnSelectStringSetup(AddonEvent type, AddonArgs args) { }

    public override void DrawConfig()
    {
        ImGuiHelper.Text(Loc.TownDispatchWorkerDesc);
        ImGui.Spacing();
        var enabled = Config.TownDispatchWorkerEnabled;
        if (ImGui.Checkbox($"{Loc.Enabled}##TownDispatch", ref enabled))
        {
            Config.TownDispatchWorkerEnabled = enabled;
            BeefChowFun.Instance.SaveConfig();
        }
        if (!Config.TownDispatchWorkerEnabled) return;
        ImGui.Spacing();
        if (ImGui.Button($"{Loc.DispatchTown}##DispatchStart")) StartDispatch();
        if (_isBusy) { ImGui.SameLine(); ImGui.TextColored(new Vector4(1,1,0,1), Loc.Dispatching); }
    }

    public override void DrawOverlay(string addonName)
    {
        if (addonName != "RetainerList") return;
        if (ImGui.Button($"{Loc.DispatchTown}##DispatchOverlay")) StartDispatch();
        if (_isBusy) { ImGui.SameLine(); ImGuiHelper.Text(Loc.Dispatching); }
    }

    public override bool ShouldDrawOverlay(string addonName)
        => addonName == "RetainerList" && Config.TownDispatchWorkerEnabled;

    private void StartDispatch()
    {
        if (_isBusy || _taskManager == null) return;
        _isBusy = true;
        Log("开始城镇派遣");
        _taskManager.Enqueue(() =>
        {
            var addon = Svc.GameGui.GetAddonByName("RetainerList");
            if (addon.Address == IntPtr.Zero) { _isBusy = false; return true; }
            var unitBase = (AtkUnitBase*)(nint)addon;
            if (!unitBase->IsVisible) { _isBusy = false; return true; }
            // 派遣: callback 2 = 派遣到城镇
            Callback.Fire(unitBase, true, 2, 0);
            return true;
        });
        _taskManager.Enqueue(() => { _isBusy = false; Log("城镇派遣完成"); return true; });
    }
}
