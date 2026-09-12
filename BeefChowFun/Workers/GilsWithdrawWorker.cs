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

internal unsafe class GilsWithdrawWorker : WorkerBase
{
    public override string Name => Loc.GilsWithdrawWorker;
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
        ImGuiHelper.Text(Loc.GilsWithdrawWorkerDesc);
        ImGui.Spacing();
        var enabled = Config.GilsWithdrawWorkerEnabled;
        if (ImGui.Checkbox($"{Loc.Enabled}##GilsWithdraw", ref enabled))
        {
            Config.GilsWithdrawWorkerEnabled = enabled;
            BeefChowFun.Instance.SaveConfig();
        }
        if (!Config.GilsWithdrawWorkerEnabled) return;
        ImGui.Spacing();
        ImGuiHelper.Text(Loc.WithdrawAmount);
        ImGui.SameLine();
        var amount = Config.WithdrawGilAmount;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt("##WithdrawAmount", ref amount))
        {
            Config.WithdrawGilAmount = Math.Max(0, amount);
            BeefChowFun.Instance.SaveConfig();
        }
        ImGui.TextDisabled("(0 = 全部提取)");
        ImGui.Spacing();
        if (ImGui.Button($"{Loc.WithdrawGils}##WithdrawStart")) StartWithdraw();
        if (_isBusy) { ImGui.SameLine(); ImGui.TextColored(new Vector4(1,1,0,1), Loc.Withdrawing); }
    }

    public override void DrawOverlay(string addonName)
    {
        if (addonName != "RetainerList") return;
        if (ImGui.Button($"{Loc.WithdrawGils}##WithdrawOverlay")) StartWithdraw();
        if (_isBusy) { ImGui.SameLine(); ImGuiHelper.Text(Loc.Withdrawing); }
    }

    public override bool ShouldDrawOverlay(string addonName)
        => addonName == "RetainerList" && Config.GilsWithdrawWorkerEnabled;

    private void StartWithdraw()
    {
        if (_isBusy || _taskManager == null) return;
        _isBusy = true;
        Log("开始提取金币");
        _taskManager.Enqueue(() =>
        {
            var addon = Svc.GameGui.GetAddonByName("RetainerList");
            if (addon.Address == IntPtr.Zero) { _isBusy = false; return true; }
            var unitBase = (AtkUnitBase*)(nint)addon;
            if (!unitBase->IsVisible) { _isBusy = false; return true; }
            // 提取金币 callback: 11 = 金币操作
            Callback.Fire(unitBase, true, 11, 0);
            return true;
        });
        _taskManager.Enqueue(() => { _isBusy = false; Log("提取金币完成"); return true; });
    }
}
