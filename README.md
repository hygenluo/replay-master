# 重放大师（ReplayMaster）

《杀戮尖塔 2》Mod：每名角色**新开局**卡组中额外加入 1 张无色技能牌「重放大师」——**固有**，耗能 1（升级后仍为 1）；打出后从**手牌**选择一张其他牌，为其叠加 **重放** 层数（基础 2，升级后 3）。机制与原版「Hidden Gem」一致，目标由随机抽牌堆改为玩家自选手牌。

## 依赖

- 游戏本体（本工程默认数据目录：`data_sts2_windows_x86_64`）
- [BaseLib](https://github.com/Alchyr/BaseLib-StS2) **0.2.0**（`csproj` 固定 NuGet 版本；构建时会复制 `BaseLib.dll` / `BaseLib.pck` / `BaseLib.json` 到 `mods/BaseLib/`）

## 外部教程

- 在线：[杀戮尖塔2mod制作教程](https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/)（Reme）
- 本地：仓库内 `SlayTheSpire2ModdingTutorials` 目录（若已克隆）

## 配置路径

在 [ReplayMaster.csproj](ReplayMaster.csproj) 中可按需修改：

- `Sts2Dir`：游戏根目录（默认 Steam 常见安装路径）
- 输出目录：`$(Sts2Dir)/mods/ReplayMaster/`

## 构建

```powershell
cd replay-master
dotnet build -c Debug
```

成功后会复制：

- `ReplayMaster.dll`、`mod_manifest.json` → `mods/ReplayMaster/`
- BaseLib 三件套 → `mods/BaseLib/`

## 导出 .pck（Godot 4.5.1 .NET）

资源（卡图、本地化、`mod_image.png`）需打入 `ReplayMaster.pck`：

1. 用 **Godot 4.5.1 Mono** 打开本目录工程。
2. **项目 → 导出**，使用预设 **BasicExport**（见 [export_presets.cfg](export_presets.cfg)）。
3. 导出为 `ReplayMaster.pck`，放入 `mods/ReplayMaster/`（与 dll 同目录）。

若未安装 Godot，仅改 C# 时游戏仍可加载 dll，但**自定义卡图/本地化可能缺失**，请先导出 pck。

**本地化与 PCK（重要）**：Mod 文案放在 [`ReplayMaster/localization/{lang}/`](ReplayMaster/localization/)。游戏只会把 mod 内容**合并进原版已存在的表文件**（例如 `cards.json`），见 `LocManager.LoadTablesFromPath` + `ModManager.GetModdedLocTables`。因此选牌提示使用表 **`cards`**、键 **`REPLAYMASTER_HAND_SELECT_REPLAY`**（与 [`ReplayMasterCard.cs`](Scripts/ReplayMasterCard.cs) 一致），写在 **`zhs/cards.json` 与 `eng/cards.json`**。勿单独新建原版没有的表文件名并指望被加载。**改 localization 后须重新导出 `ReplayMaster.pck`**。

## 卡图

默认使用 [ReplayMaster/images/cards/ReplayMaster.png](ReplayMaster/images/cards/ReplayMaster.png)。可将你的立绘覆盖该文件并在 Godot 中重新导入后导出 pck。

## 故障排除（BaseLib 版本 / 旧 DLL）

若日志出现 **`BaseLib, Version=0.2.1.0`** 找不到，而游戏中已是 BaseLib **0.2.0**：说明 **`mods/ReplayMaster/ReplayMaster.dll` 仍是旧编译产物**。请在工程目录执行 **`dotnet clean -c Debug`** 后再 **`dotnet build -c Debug`**，确认 PostBuild 覆盖了游戏目录下的 dll；并用资源管理器查看 **`ReplayMaster.dll` 修改时间**是否为刚编译时间。

`mods/ReplayMaster/` 内**只应保留清单 `mod_manifest.json`**。若仍有旧的 **`ReplayMaster.json`**，请删除，以免与当前约定冲突。

## 故障排除（日志：Unknown ID / 只加载了一个 mod）

- 若出现 **`Unknown ID: CARD.REPLAYMASTER-REPLAY_MASTER_CARD, skipping`**：说明进度里记着这张卡，但**本次启动未注册**该卡。最常见原因是**只启用了 BaseLib、未启用 ReplayMaster**，或 ReplayMaster 的 dll 未部署。请在游戏 Mod 界面**同时勾选 BaseLib 与 ReplayMaster**，并确认 `mods/ReplayMaster/` 下有 `ReplayMaster.dll` 与 `mod_manifest.json`。
- 成功加载时，`godot.log` 中 mod 数量应对应**至少两个**相关 mod，并可见 **`ReplayMaster mod initialized`**（`Log.Info`）。
- 退出时 **Godot leak** 警告：可先按 [DEBUG.md](DEBUG.md) 中「泄漏基线」做三次对比（原版 / +BaseLib / +ReplayMaster），再判断是否与本 mod 相关。

## 故障排除（LocException：选牌文案 / `cards` 表）

- 若出牌时 **`LocException`** 与某 loc 表或 key 相关：确认 [`ReplayMaster/localization/zhs/cards.json`](ReplayMaster/localization/zhs/cards.json) 与 [`ReplayMaster/localization/eng/cards.json`](ReplayMaster/localization/eng/cards.json) 含 **`REPLAYMASTER_HAND_SELECT_REPLAY`**，且已**重新导出 PCK** 并部署。日志中应出现对 **`cards.json`** 的 mod merge（`Found loc table from mod: ... cards.json`）。

## 游戏内测试

1. 确认 `mods` 下已启用 **BaseLib** 与 **ReplayMaster**，且含 `ReplayMaster.pck`。
2. 新开一局任意角色，卡组中应多出一张「重放大师」。
3. 战斗中第一回合**固有**应入手（若牌组已洗入）。
4. 打出后选择手牌中另一张牌，检查其 **重放** 层数（2 / 升级 3）。

控制台（`~`）可尝试：

```text
card REPLAYMASTER-REPLAY_MASTER_CARD
```

（具体 ID 以 BaseLib 规则为准：命名空间首段 + 类名 snake_case。）

## 文档

- [docs/完整编写流程.md](docs/完整编写流程.md)
- [DEBUG.md](DEBUG.md)
