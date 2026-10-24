# Madby增强（MadbyEnhance）

一个 Emby 小插件：精简影片详情页显示的内容。服务端下发数据时统一过滤，**所有客户端同时生效**（Yamby / Hills / AfuseKt / Emby 官方 App / 网页版）。

## 功能开关

| 开关 | 作用 |
|---|---|
| 只显示演员（含剧集演员） | 人物列表只保留 Actor / GuestStar，隐藏导演、编剧、制片等 |
| 隐藏外部链接 | 清空 `ExternalUrls`（IMDb / TMDB / Trakt 等） |
| 隐藏工作室 | 清空 `Studios`（制片 / 发行方一行） |
| 隐藏标签 | 清空 `Tags` / `TagItems`（客户端「按标签筛选」也会变空） |
| 隐藏相关推荐 | 清空 `/Items/{id}/Similar` 的返回，详情页「相关推荐 / 更多类似」为空 |

所有开关默认开启，改完即时生效（配置页勾选会自动保存）。只影响展示，不修改媒体库数据。

## 原理

通过运行时反射找到已加载的程序集，用 Madby 插件带入进程的 Harmony（`0Harmony`）打补丁：

- `Emby.Server.Implementations.Dto.DtoService.AttachPeople` → 过滤 `People`
- `Emby.Server.Implementations.Dto.DtoService.AttachBasicFields` → 清空外链 / 工作室 / 标签
- `Emby.Api.Library.LibraryService.GetSimilarItemsResult` → 清空相关推荐结果

因此**依赖 Madby 插件已安装并启用**（它提供 Harmony 运行库）；没有 Madby 时页面能打开，但补丁不会生效。

## 安装

1. 把 `MadbyEnhance.dll` 放进 Emby 配置目录下的 `plugins/`（Docker 常见路径 `/config/plugins/`）。
2. 重启 Emby。
3. Emby 后台 → 设置 → 高级 → Madby增强 → 按需勾选。

## 构建

```bash
# 1. 从你的 Emby 服务器复制这三个程序集到 refs/
#    （Emby 安装目录 / 容器内 /system/ 下）
#    MediaBrowser.Model.dll  MediaBrowser.Controller.dll  MediaBrowser.Common.dll
# 2. 构建
dotnet build src/OnlyActors/OnlyActors.csproj -c Release -o out
```

需要 .NET 8 SDK。仓库**不包含** Emby 的任何程序集，请自行从自己的 Emby 服务器提取。

## 卸载 / 回滚

删掉 `plugins/MadbyEnhance.dll` 并重启 Emby 即可；对媒体库、播放、Madby 本身无影响。
