using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Numerics;
using Callback = ECommons.Automation.Callback;

namespace BeefChowFun.Workers;

/// <summary>
/// 收取雇员 Worker
/// 自动收取所有雇员完成的任务
/// </summary>
internal unsafe class CollectWorker : WorkerBase
{
    public override string Name => Loc.CollectWorker;

    private TaskManager? _taskManager;
    private bool _isBusy;
    public override bool IsBusy => _isBusy;

    // 当前处理的雇员索引
    private int _currentRetainerIndex = 0;
    private int _totalRetainers = 0;

    public override void Init()
    {
        _taskManager = new TaskManager(new(abortOnTimeout: true, timeLimitMS: 30000, showDebug: false));
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerList", OnRetainerListSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", OnSelectStringSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerTaskResult", OnRetainerTaskResultSetup);
    }

    public override void Uninit()
    {
        _taskManager?.Abort();
        _taskManager = null;
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerList", OnRetainerListSetup);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "SelectString", OnSelectStringSetup);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerTaskResult", OnRetainerTaskResultSetup);
    }

    private void OnRetainerListSetup(AddonEvent type, AddonArgs args) { }

    private void OnSelectStringSetup(AddonEvent type, AddonArgs args)
    {
        if (!_isBusy || _taskManager == null) return;
        // 当 SelectString 打开时（雇员菜单），自动点击"任务结果"
        _taskManager.Enqueue(() => ClickRetainerMenuTaskResult());
    }

    private void OnRetainerTaskResultSetup(AddonEvent type, AddonArgs args)
    {
        if (!_isBusy || _taskManager == null) return;
        // 收取任务结果
        _taskManager.Enqueue(() => ConfirmTaskResult());
    }

    public override void DrawConfig()
    {
        ImGuiHelper.Text(Loc.CollectWorkerDesc);
        ImGui.Spacing();

        var enabled = Config.CollectWorkerEnabled;
        if (ImGui.Checkbox($"{Loc.Enabled}##CollectWorker", ref enabled))
        {
            Config.CollectWorkerEnabled = enabled;
            BeefChowFun.Instance.SaveConfig();
        }

        if (!Config.CollectWorkerEnabled) return;
        ImGui.Spacing();

        using var disabled = new ImGuiDisabledScope(_isBusy);
        if (ImGui.Button($"{Loc.CollectAll}##CollectWorkerStart"))
            StartCollect();

        if (_isBusy)
        {
            ImGui.SameLine();
            ImGuiHelper.TextColored(new Vector4(1, 1, 0, 1), $"{Loc.Collecting} ({_currentRetainerIndex}/{_totalRetainers})");
        }
    }

    public override void DrawOverlay(string addonName)
    {
        if (addonName != "RetainerList") return;
        if (ImGui.Button($"{Loc.CollectAll}##CollectOverlay"))
            StartCollect();
        if (_isBusy)
        {
            ImGui.SameLine();
            ImGuiHelper.Text($"{Loc.Collecting} {_currentRetainerIndex}/{_totalRetainers}");
        }
    }

    public override bool ShouldDrawOverlay(string addonName)
        => addonName == "RetainerList" && Config.CollectWorkerEnabled;

    private void StartCollect()
    {
        if (_isBusy || _taskManager == null) return;

        var addon = Svc.GameGui.GetAddonByName("RetainerList");
        if (addon == IntPtr.Zero) { LogError(Loc.ErrorRetainerListNotOpen); return; }

        _isBusy = true;
        _currentRetainerIndex = 0;
        Log("开始收取雇员任务");

        // 枚举所有雇员并逐一收取
        _taskManager.Enqueue(() => EnumerateAndCollect());
        _taskManager.Enqueue(() => { _isBusy = false; Log("收取完成"); return true; });
    }

    private bool? EnumerateAndCollect()
    {
        var addon = Svc.GameGui.GetAddonByName("RetainerList");
        if (addon.Address == IntPtr.Zero) { _isBusy = false; return true; }
        var unitBase = (AtkUnitBase*)(nint)addon;
        if (!unitBase->IsVisible) { _isBusy = false; return true; }

        // 获取雇员列表
        var retainerList = (AddonRetainerList*)unitBase;
        _totalRetainers = 0;

        // 计算已完成任务的雇员数量
        for (int i = 0; i < 10; i++)
        {
            var node = unitBase->GetNodeById((uint)(4 + i));
            if (node == null || !node->IsVisible()) break;
            _totalRetainers++;
        }

        if (_totalRetainers == 0) { _isBusy = false; return true; }

        // 逐一点击每个雇员
        for (int i = 0; i < _totalRetainers; i++)
        {
            int idx = i;
            _taskManager!.Enqueue(() =>
            {
                if (!EzThrottler.Throttle("CollectRetainer", Config.OperationDelay)) return false;
                var a = Svc.GameGui.GetAddonByName("RetainerList");
                if (a.Address == IntPtr.Zero) return true;
                var u = (AtkUnitBase*)(nint)a;
                _currentRetainerIndex = idx + 1;
                // 点击雇员列表中的雇员（callback 2 = 选择雇员）
                Callback.Fire(u, true, 2, idx);
                return true;
            });
            // 等待 SelectString 打开并处理
            _taskManager!.EnqueueDelay(Config.OperationDelay);
        }

        return true;
    }

    private bool? ClickRetainerMenuTaskResult()
    {
        if (!EzThrottler.Throttle("ClickMenu", Config.OperationDelay)) return false;
        var addon = Svc.GameGui.GetAddonByName("SelectString");
        if (addon.Address == IntPtr.Zero) return false;
        var unitBase = (AtkUnitBase*)(nint)addon;
        if (!unitBase->IsVisible) return false;
        // 菜单选项: 0=当前派遣, 1=查看投资评估, 2=市场销售, 3=任务结果
        // 尝试找到"任务结果"选项
        Callback.Fire(unitBase, true, 3, 3); // 选中"任务结果"(index 3)
        return true;
    }

    private bool? ConfirmTaskResult()
    {
        if (!EzThrottler.Throttle("ConfirmResult", Config.OperationDelay)) return false;
        var addon = Svc.GameGui.GetAddonByName("RetainerTaskResult");
        if (addon.Address == IntPtr.Zero) return false;
        var unitBase = (AtkUnitBase*)(nint)addon;
        if (!unitBase->IsVisible) return false;
        // 确认收取（callback 1 = 确认）
        Callback.Fire(unitBase, true, 1);
        return true;
    }

    /// <summary>
    /// ImGui 禘1助类，用于 BeginDisabled/EndDisabled
    /// </summary>
    private readonly struct ImGuiDisabledScope : IDisposable
    {
        private readonly bool _disabled;
        public ImGuiDisabledScope(bool disabled) { _disabled = disabled; if (_disabled) ImGui.BeginDisabled(); }
        public void Dispose() { if (_disabled) ImGui.EndDisabled(); }
    }
}
