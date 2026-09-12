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

internal unsafe class GilsShareWorker : WorkerBase
{
    public override string Name => Loc.GilsShareWorker;
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
        ImGuiHelper.Text(Loc.GilsShareWorkerDesc);
        ImGui.Spacing();
        var enabled = Config.GilsShareWorkerEnabled;
        if (ImGui.Checkbox($"{Loc.Enabled}##GilsShare", ref enabled))
        {
            Config.GilsShareWorkerEnabled = enabled;
            BeefChowFun.Instance.SaveConfig();
        }
        if (!Config.GilsShareWorkerEnabled) return;
        ImGui.Spacing();
        ImGuiHelper.Text(Loc.ShareAmount);
        ImGui.SameLine();
        var amount = Config.ShareGilAmount;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt("##ShareAmount", ref amount))
        {
            Config.ShareGilAmount = Math.Max(0, amount);
            BeefChowFun.Instance.SaveConfig();
        }
        ImGui.TextDisabled("(0 = 平均分配)");
        ImGui.Spacing();
        if (ImGui.Button($"{Loc.ShareGils}##ShareStart")) StartShare();
        if (_isBusy) { ImGui.SameLine(); ImGui.TextColored(new Vector4(1,1,0,1), Loc.Sharing); }
    }

    public override void DrawOverlay(string addonName)
    {
        if (addonName != "RetainerList") return;
        if (ImGui.Button($"{Loc.ShareGils}##ShareOverlay")) StartShare();
        if (_isBusy) { ImGui.SameLine(); ImGuiHelper.Text(Loc.Sharing); }
    }

    public override bool ShouldDrawOverlay(string addonName)
        => addonName == "RetainerList" && Config.GilsShareWorkerEnabled;

    private void StartShare()
    {
        if (_isBusy || _taskManager == null) return;
        _isBusy = true;
        Log("开始分配金币");
        _taskManager.Enqueue(() =>
        {
            var addon = Svc.GameGui.GetAddonByName("RetainerList");
            if (addon.Address == IntPtr.Zero) { _isBusy = false; return true; }
            var unitBase = (AtkUnitBase*)(nint)addon;
            if (!unitBase->IsVisible) { _isBusy = false; return true; }
            Callback.Fire(unitBase, true, 11, 0);
            return true;
        });
        _taskManager.Enqueue(() => { _isBusy = false; Log("分配金币完成"); return true; });
    }
}
