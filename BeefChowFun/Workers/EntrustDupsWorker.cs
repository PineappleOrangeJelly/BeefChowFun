using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;

namespace BeefChowFun.Workers;

/// <summary>
/// 委托重复物品 Worker
/// 自动将背包中的重复物品委托给雇员存储
/// </summary>
internal unsafe class EntrustDupsWorker : WorkerBase
{
    public override string Name => Loc.EntrustDupsWorker;

    private TaskManager? _taskManager;
    private bool _isBusy;
    public override bool IsBusy => _isBusy;

    public override void Init()
    {
        _taskManager = new TaskManager(new(abortOnTimeout: true, timeLimitMS: 10000, showDebug: false));
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerList", OnRetainerListSetup);
    }

    public override void Uninit()
    {
        _taskManager?.Abort();
        _taskManager = null;
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerList", OnRetainerListSetup);
    }

    private void OnRetainerListSetup(AddonEvent type, AddonArgs args) { }

    public override void DrawConfig()
    {
        ImGuiHelper.Text(Loc.EntrustDupsWorkerDesc);
        ImGui.Spacing();
        var enabled = Config.EntrustDupsWorkerEnabled;
        if (ImGui.Checkbox($"{Loc.Enabled}##EntrustDups", ref enabled))
        {
            Config.EntrustDupsWorkerEnabled = enabled;
            BeefChowFun.Instance.SaveConfig();
        }
        if (!Config.EntrustDupsWorkerEnabled) return;
        ImGui.Spacing();
        if (ImGui.Button($"{Loc.EntrustDups}##EntrustDupsStart"))
            StartEntrust();
        if (_isBusy) { ImGui.SameLine(); ImGui.TextColored(new System.Numerics.Vector4(1,1,0,1), Loc.Entrusting); }
    }

    public override void DrawOverlay(string addonName)
    {
        if (addonName != "RetainerList") return;
        if (ImGui.Button($"{Loc.EntrustDups}##EntrustDupsOverlay"))
            StartEntrust();
        if (_isBusy) { ImGui.SameLine(); ImGuiHelper.Text(Loc.Entrusting); }
    }

    public override bool ShouldDrawOverlay(string addonName)
        => addonName == "RetainerList" && Config.EntrustDupsWorkerEnabled;

    private void StartEntrust()
    {
        if (_isBusy || _taskManager == null) return;
        _isBusy = true;
        Log("开始委托重复物品");

        _taskManager.Enqueue(() => EntrustDuplicatesOnCurrentRetainer());
        _taskManager.Enqueue(() =>
        {
            _isBusy = false;
            Log("委托重复物品完成");
            return true;
        });
    }

    private bool? EntrustDuplicatesOnCurrentRetainer()
    {
        var addon = Svc.GameGui.GetAddonByName("RetainerList");
        if (addon == IntPtr.Zero) { _isBusy = false; return true; }
        var unitBase = (AtkUnitBase*)addon.Address;
        if (!unitBase->IsVisible) { _isBusy = false; return true; }
        // 单击"委托重复物品"按鈕（如果存在）
        // RetainerList 调用方式：63=委托重复物品
        ECommons.Automation.Callback.Fire(unitBase, true, 12, 0);
        return true;
    }
}
