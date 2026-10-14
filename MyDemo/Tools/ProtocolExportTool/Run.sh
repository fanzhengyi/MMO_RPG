#!/bin/bash

SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)
cd "$SCRIPT_DIR" || exit 1

# 保留项目维护的客户端 helper；成功或失败都恢复原文件。
HELPER="$SCRIPT_DIR/../../Client/MMO_RPG/Assets/MMO_Hot/Proto/NetworkProtocolHelper.cs"
BACKUP=$(mktemp) || exit 1
cp -- "$HELPER" "$BACKUP" || { rm -f -- "$BACKUP"; exit 1; }
restore_helper() {
    cp -- "$BACKUP" "$HELPER" || return 1
    rm -f -- "$BACKUP"
}
trap restore_helper EXIT

echo "=========================================="
echo "Fantasy Protocol Export Tool 2026.0.1025"
echo "=========================================="
echo ""

dotnet "$SCRIPT_DIR/Fantasy.ProtocolExportTool.dll" export --silent
EXPORT_RESULT=$?
if [ "$EXPORT_RESULT" -eq 0 ]; then
    for file in "$SCRIPT_DIR/../../Client/MMO_RPG/Assets/MMO_Hot/Proto/"*.cs \
                "$SCRIPT_DIR/../../Server/Entity/Generate/NetworkProtocol/"*.cs; do
        sed -i 's/[[:blank:]]\+$//' "$file" || { EXPORT_RESULT=1; break; }
    done
fi
restore_helper || exit 1
trap - EXIT

echo "按回车键退出..."
read -r
exit "$EXPORT_RESULT"
