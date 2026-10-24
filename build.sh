#!/usr/bin/env sh
# 构建 MadbyEnhance：需先把 Emby 的 MediaBrowser.*.dll 放到 refs/
set -e
cd "$(dirname "$0")"
for f in MediaBrowser.Model.dll MediaBrowser.Controller.dll MediaBrowser.Common.dll; do
  [ -f "refs/$f" ] || { echo "缺少 refs/$f（从 Emby 服务器 /system/ 复制过来）"; exit 1; }
done
dotnet build src/OnlyActors/OnlyActors.csproj -c Release -o out
echo "产物: out/MadbyEnhance.dll"
