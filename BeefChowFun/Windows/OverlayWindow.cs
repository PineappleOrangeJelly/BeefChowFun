using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace BeefChowFun.Windows;

internal unsafe class OverlayWindow : Window, IDisposable
{
    private readonly BeefChowFun _plugin;
    private string _currentAddonName = string.Empty;

    public OverlayWindow(BeefChowFun plugin)
        : base(Loc.OverlayTitle,
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoFocusOnAppearing)
    {
        _plugin = plugin;
        IsOpen = false;

        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerList", OnAddonShow);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "RetainerList", OnAddonHide);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", OnAddonShow);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "SelectString", OnAddonHide);
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerList", OnAddonShow);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "RetainerList", OnAddonHide);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "SelectString", OnAddonShow);
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "SelectString", OnAddonHide);
    }

    private void OnAddonShow(AddonEvent type, AddonArgs args)
    {
        if (!_plugin.Configuration.Enabled) return;
        _currentAddonName = args.AddonName;
        IsOpen = true;
        UpdatePosition();
    }

    private void OnAddonHide(AddonEvent type, AddonArgs args)
    {
        if (args.AddonName == _currentAddonName)
        {
            IsOpen = false;
            _currentAddonName = string.Empty;
        }
    }

    private void UpdatePosition()
    {
        var addon = Svc.GameGui.GetAddonByName(_currentAddonName);
        if (addon.Address == IntPtr.Zero) return;

        var unitBase = (AtkUnitBase*)(nint)addon;
        if (!unitBase->IsVisible) return;

        var pos = new Vector2(unitBase->X + unitBase->GetScaledWidth(true) + 4, unitBase->Y);
        Position = pos;
    }

    public override void PreDraw()
    {
        UpdatePosition();
    }

    public override void Draw()
    {
        if (!_plugin.Configuration.Enabled) return;
        if (string.IsNullOrEmpty(_currentAddonName)) return;

        bool anyDrawn = false;
        foreach (var worker in _plugin.Workers)
        {
            if (!worker.ShouldDrawOverlay(_currentAddonName)) continue;
            worker.DrawOverlay(_currentAddonName);
            anyDrawn = true;
            ImGui.Separator();
        }

        if (!anyDrawn)
        {
            ImGui.TextDisabled("无可用操作");
        }
    }
}
