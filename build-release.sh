#!/bin/bash

# BeefChowFun Release 打包脚本
# 用于自动构建和打包插件发布版本

set -e

echo "========================================"
echo "  干炒牛河 (BeefChowFun) 打包脚本"
echo "========================================"
echo ""

# 颜色定义
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# 项目路径
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$PROJECT_DIR/BeefChowFun"
RELEASE_DIR="$PROJECT_DIR/release"
PLUGIN_DIR="$RELEASE_DIR/BeefChowFun"

echo -e "${YELLOW}项目目录:${NC} $PROJECT_DIR"
echo -e "${YELLOW}构建目录:${NC} $BUILD_DIR"
echo -e "${YELLOW}发布目录:${NC} $RELEASE_DIR"
echo ""

# 步骤 1: 清理旧的发布文件
echo -e "${YELLOW}[1/4]${NC} 清理旧的发布文件..."
rm -rf "$PLUGIN_DIR"
mkdir -p "$PLUGIN_DIR"
echo -e "${GREEN}✓${NC} 清理完成"
echo ""

# 步骤 2: 构建项目
echo -e "${YELLOW}[2/4]${NC} 构建项目..."
cd "$BUILD_DIR"
dotnet build -c Release
if [ $? -ne 0 ]; then
    echo -e "${RED}✗ 构建失败！${NC}"
    exit 1
fi
echo -e "${GREEN}✓${NC} 构建成功"
echo ""

# 步骤 3: 复制必要文件
echo -e "${YELLOW}[3/4]${NC} 复制发布文件..."
BIN_DIR="$BUILD_DIR/bin/Release"

# 必需的文件列表
FILES=(
    "BeefChowFun.dll"
    "BeefChowFun.json"
    "ECommons.dll"
    "ECommons.IPC.dll"
    "ImGui.NET.dll"
)

for file in "${FILES[@]}"; do
    if [ -f "$BIN_DIR/$file" ]; then
        cp -v "$BIN_DIR/$file" "$PLUGIN_DIR/"
        echo -e "${GREEN}✓${NC} 已复制: $file"
    else
        echo -e "${RED}✗ 缺失文件: $file${NC}"
        exit 1
    fi
done
echo ""

# 步骤 4: 生成文件信息
echo -e "${YELLOW}[4/4]${NC} 生成发布信息..."
cd "$PLUGIN_DIR"

echo "==================================" > "$PLUGIN_DIR/FILES.txt"
echo "  干炒牛河 v1.0.0.0 文件清单" >> "$PLUGIN_DIR/FILES.txt"
echo "  生成时间: $(date '+%Y-%m-%d %H:%M:%S')" >> "$PLUGIN_DIR/FILES.txt"
echo "==================================" >> "$PLUGIN_DIR/FILES.txt"
echo "" >> "$PLUGIN_DIR/FILES.txt"

for file in "${FILES[@]}"; do
    size=$(du -h "$file" | cut -f1)
    echo "$file ($size)" >> "$PLUGIN_DIR/FILES.txt"
done

total_size=$(du -sh . | cut -f1)
echo "" >> "$PLUGIN_DIR/FILES.txt"
echo "总大小: $total_size" >> "$PLUGIN_DIR/FILES.txt"

echo -e "${GREEN}✓${NC} 文件清单已生成"
echo ""

# 显示最终结果
echo "========================================"
echo -e "${GREEN}  打包完成！${NC}"
echo "========================================"
echo ""
echo "发布文件位置: $PLUGIN_DIR"
echo "文件列表:"
ls -lh "$PLUGIN_DIR"
echo ""
echo "总大小: $total_size"
echo ""
echo -e "${YELLOW}下一步：${NC}"
echo "1. 检查 release/ 目录中的文件"
echo "2. 提交到 GitHub: git add release/ && git commit -m 'Release v1.0.0.0'"
echo "3. 创建 GitHub Release 并上传 BeefChowFun 文件夹"
echo ""
