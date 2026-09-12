#!/bin/bash
# BeefChowFun 快速部署脚本

set -e

echo "正在编译 BeefChowFun..."
cd BeefChowFun
dotnet build -c Release

echo "正在部署到游戏目录..."
DEST="$HOME/.xlcore_cn/devPlugins/BeefChowFun"
mkdir -p "$DEST"

# 复制所有 DLL（除了 ImGui.NET.dll，它由 Dalamud 提供）
cp -v bin/Release/*.dll "$DEST/" 2>/dev/null || true
rm -f "$DEST/ImGui.NET.dll"

# 复制主 DLL（确保是最新的）
cp -v bin/Release/BeefChowFun.dll "$DEST/"

# 复制配置文件
cp -v BeefChowFun.json "$DEST/"

echo "✓ 部署完成！"
echo "请在游戏中重新加载插件或重启游戏来测试。"
echo "使用 /bcf 命令打开配置窗口。"
