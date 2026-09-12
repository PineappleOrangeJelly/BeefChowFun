using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace BeefChowFun.Windows;

internal class ConfigWindow : Window, IDisposable
{
    private readonly BeefChowFun _plugin;

    public ConfigWindow(BeefChowFun plugin)
        : base(Loc.ConfigWindowTitle, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        _plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 400),
            MaximumSize = new Vector2(800, 900)
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var config = _plugin.Configuration;

        // 全局开关
        var enabled = config.Enabled;
        if (ImGui.Checkbox($"{Loc.Enabled}##Global", ref enabled))
        {
            config.Enabled = enabled;
            _plugin.SaveConfig();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("(使用 /bcf 开关此窗口)");

        ImGui.Separator();

        // Tab栏
        if (ImGui.BeginTabBar("##WorkerTabs"))
        {
            foreach (var worker in _plugin.Workers)
            {
                if (!worker.ShouldDrawConfig()) continue;
                if (ImGui.BeginTabItem($"{worker.Name}##Tab"))
                {
                    ImGui.Spacing();
                    worker.DrawConfig();
                    ImGui.EndTabItem();
                }
            }

            // 高级设置 Tab
            if (ImGui.BeginTabItem("高级##AdvancedTab"))
            {
                ImGui.Spacing();
                DrawAdvancedSettings();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawAdvancedSettings()
    {
        var config = _plugin.Configuration;

        ImGui.TextUnformatted("操作延迟");
        var delay = config.OperationDelay;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderInt("##Delay", ref delay, 100, 2000))
        {
            config.OperationDelay = delay;
            _plugin.SaveConfig();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("ms");

        ImGui.Spacing();

        var debug = config.EnableDebugLog;
        if (ImGui.Checkbox("启用调试日志##Debug", ref debug))
        {
            config.EnableDebugLog = debug;
            _plugin.SaveConfig();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextDisabled("BeefChowFun v1.0.0 by ccc");
        ImGui.TextDisabled("移植自 DailyRoutines AutoRetainerWork");
    }
}
