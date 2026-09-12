using Dalamud.Bindings.ImGui;

namespace BeefChowFun;

/// <summary>
/// ImGui 辅助类 - 处理中文字符串显示
/// 使用 Dalamud 的 ImU8String 类型确保 UTF-8 编码正确处理
/// </summary>
internal static class ImGuiHelper
{
    /// <summary>
    /// 显示文本（支持中文）
    /// </summary>
    public static void Text(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        ImGui.Text(new ImU8String(text));
    }

    /// <summary>
    /// 显示彩色文本（支持中文）
    /// </summary>
    public static void TextColored(System.Numerics.Vector4 color, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        ImGui.TextColored(color, new ImU8String(text));
    }

    /// <summary>
    /// 显示灰色文本（支持中文）
    /// </summary>
    public static void TextDisabled(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        ImGui.TextDisabled(new ImU8String(text));
    }
}
