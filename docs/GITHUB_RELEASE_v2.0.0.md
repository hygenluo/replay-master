## 安装

将 `mod_manifest.json`、`ReplayMaster.dll`、`ReplayMaster.pck` 放入游戏目录 `Slay the Spire 2/mods/ReplayMaster/`。需与 **BaseLib** 同时启用（构建时通过 `Version="*"` 自动解析最新稳定版，并在 NuGet 缓存中发现版本目录；构建日志输出形如 `BaseLib resolved → X.Y.Z`）。

## v2.0.0 变更摘要

- **BaseLib**：NuGet 通过 `Version="*"` 自动解析最新稳定版；部署时从 NuGet 缓存中自动发现版本目录。如需锁定版本：`dotnet build /p:BaseLibNuGetVersion=0.2.6`。
- **本地化修复**：在 `cards` 表中补充引擎使用的键 `REPLAY_MASTER_CARD.title` / `REPLAY_MASTER_CARD.description`（与 `CardModel` 的 `Id.Entry` 规则一致），避免 `LocException` 与卡牌排序时的 `Failed to compare two elements in the array`。**更新后请重新用 Godot 导出 `ReplayMaster.pck` 再发布或安装。**

## 相对 v1.1.0

- 清单与程序集版本 **2.0.0**。

## 卡牌效果（简述）

**重放大师**：稀有无色技能，费用 1。选择手牌中另一张牌，使其获得 **+2** 重放（升级 **+3**）。固有、保留；打出后本卡回到手牌。
