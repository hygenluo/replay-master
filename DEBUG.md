# DEBUG 记录（重放大师）

每次排错请追加一节，便于回归与版本对照。

## 记录模板

- **日期**：
- **现象**：
- **复现步骤**：
- **日志 / 截图**：
- **原因**：
- **修改**（文件、补丁、配置）：
- **验证结果**：

---

## 2026-03-29 — 清理重建：消除对 BaseLib 0.2.1 的程序集引用

- **现象**：`godot.log` 报 `Could not load file or assembly 'BaseLib, Version=0.2.1.0'`，同时 BaseLib 已正确加载为 0.2.0；`Loaded 1 mods (2 total)`。
- **原因**：游戏目录中的 `ReplayMaster.dll` 为旧编译产物（仍绑定 0.2.1）；且 `mods/ReplayMaster/` 下残留 **`ReplayMaster.json`**（旧清单），与 `mod_manifest.json` 并存，日志仍可能扫描到旧文件。
- **修改**：在项目目录执行 `dotnet clean -c Debug` 后 `dotnet restore` + `dotnet build -c Debug`（PostBuild 覆盖 `mods/ReplayMaster/ReplayMaster.dll`）；**删除**游戏目录 `mods/ReplayMaster/ReplayMaster.json`。
- **验证**：`ReplayMaster.deps.json` 中 `Alchyr.Sts2.BaseLib` 为 0.2.0、`assemblyVersion` 0.2.0.0；对 `ReplayMaster.dll` 二进制搜索无 `0.2.1`；`ReplayMaster.dll` 与 `mod_manifest.json` 时间戳为最新。

---

## 2026-03-29 — BaseLib 0.2.0 与 mod_manifest.json

- **现象**：需固定 BaseLib 为 0.2.0；清单需用 `mod_manifest.json` 供游戏识别。
- **修改**：`ReplayMaster.csproj` 中 `Alchyr.Sts2.BaseLib` 改为 `0.2.0`；`ReplayMaster.json` 重命名为 `mod_manifest.json`；PostBuild / `export_presets.cfg` exclude / README / 本流程文档同步。
- **验证结果**：本地 `dotnet build` 需通过；游戏中 `mods/ReplayMaster/mod_manifest.json` 与 `mods/BaseLib` 三件套来自 0.2.0 包。

---

## 2026-03-29 — 首次实现与编译

- **现象**：无（实现阶段）。
- **原因**：`CardModel` 中 `CanonicalVars`、`ExtraHoverTips`、`OnPlay`、`OnUpgrade` 在公开展开 API 下为 `public virtual`，子类需 `public override` 而非 `protected override`。
- **修改**：`Scripts/ReplayMasterCard.cs` 访问修饰符改为 `public override`。
- **验证结果**：`dotnet build -c Debug` 通过；Harmony 开局补丁与卡牌逻辑需在实机加载 Mod 后确认。

---

## 2026-03-29 — 执行计划：双 Mod、Unknown ID、泄漏基线、选牌日志

### 1. 同时启用 BaseLib + ReplayMaster（必须）

- **现象**：`godot.log` 出现 `Loaded 1 mods (1 total)` 且仅有 BaseLib；或进度解析 `Unknown ID: CARD.REPLAYMASTER-REPLAY_MASTER_CARD`。
- **原因**：ReplayMaster 定义卡牌与补丁，**不会**随 BaseLib 自动加载；仅开 BaseLib 时 ModelDb 无该卡注册，旧档中的卡 ID 会被跳过。
- **操作**：游戏 Mod 列表中**同时勾选** BaseLib 与 ReplayMaster；`mods/BaseLib/` 与 `mods/ReplayMaster/` 分别含 dll+pck+json（ReplayMaster 侧为 `mod_manifest.json`）。构建部署后日志应出现约 **2 个 mod** 加载，且有一条 **`ReplayMaster mod initialized`**（`Entry` 使用 `Log.Info`）。
- **验证（本机一次核对）**：`mods/BaseLib` 含 `BaseLib.dll|pck|json`；`mods/ReplayMaster` 含 `ReplayMaster.dll|pck|mod_manifest.json`；无残留 `ReplayMaster.json`。
- **部署失败**：若 `dotnet build` 报无法 `RemoveDir` / `ReplayMaster.dll` Access denied，请先**完全退出游戏**再构建（`csproj` 会在构建时清空 `mods/ReplayMaster` 再复制）。

### 2. Unknown ID 与存档

- **接受跳过**：警告表示该条进度中的该卡引用被丢弃，可继续玩或新开档。
- **勿在仅 BaseLib 下打开**曾用 ReplayMaster 玩过的存档；否则必然 Unknown。
- **ID 稳定性**：勿随意改 `ReplayMasterCard` 类名/命名空间，否则生成新 ModelId，旧档再次 Unknown。

### 3. 退出时资源泄漏（基线对比）

- **步骤**：① 仅原版退出 ② 原版+BaseLib 退出 ③ 原版+BaseLib+ReplayMaster 退出，各看 `godot.log` 末尾泄漏条数/类型。
- **判断**：若三种情况接近 → 多为引擎/BaseLib 共性；若仅加 ReplayMaster 后明显增加 → 再查本 mod 是否持有 Node/Resource 引用。ReplayMaster 当前主要为 C# 卡逻辑 + Harmony，无自建 `NCreatureVisuals`/`NEnergyCounter` 工厂。

### 4. 选牌打勾仍异常时

- 在**两 mod 均已启用**前提下，从**进入战斗**到复现截取 `godot.log`。
- 搜索 `Player chose cards`（`CardSelectCmd` 的 `Log.Info`）与异常栈；代码侧已对手牌选择做 `ToList`、堆内引用对齐与 `NCard` 刷新（见 `ReplayMasterCard.cs`）。

---

## 2026-03-29 — LocException：`replay_master_ui` 表不存在

- **现象**：打出「重放大师」时 `LocException: The loc table='replay_master_ui' does not exist`；栈在 `ReplayMasterCard.OnPlay` 的 `CardSelectorPrefs` / `LocString`；随后可能出现 `Task` 已完成后再次 `SetResult`（选牌确认连锁）。
- **原因**：`OnPlay` 使用 `new LocString("replay_master_ui", "HAND_SELECT_REPLAY")`，运行时需从 **PCK** 加载对应语言表。曾仅有 `zhs/replay_master_ui.json`，**英文界面**或缺少 `eng` 表即报表不存在；或 **PCK 未重新导出**，包内无该路径。
- **修改（已过时）**：曾尝试新增独立 `replay_master_ui.json`；见下一节「根因修正」——引擎不会加载原版不存在的表文件名。
- **验证**：英/中文界面各打出一局并选手牌，无 `LocException`；`godot.log` 可见 mod 加载相关本地化；确认无 confirm 二次异常。

---

## 2026-03-29 — 根因修正：mod 仅能合并「原版已有」的 loc 文件名

- **现象**：已补 `replay_master_ui.json` 并重新导出 PCK 后，日志仍只有 `Found loc table from mod: zhs cards.json`，无 `replay_master_ui`，出牌仍报 `replay_master_ui` 不存在。
- **原因**：`LocManager.LoadTablesFromPath` 只遍历**主游戏** `res://localization/{lang}/` 下已有 json 文件名，再对每个文件名调用 `ModManager.GetModdedLocTables` 合并 mod。原版没有 `replay_master_ui.json`，循环永远不会处理该文件名，**独立表不会被加载**。
- **修改**：选牌提示改为 `LocString("cards", "REPLAYMASTER_HAND_SELECT_REPLAY")`；键写入 [`ReplayMaster/localization/zhs/cards.json`](ReplayMaster/localization/zhs/cards.json) 与新建 [`ReplayMaster/localization/eng/cards.json`](ReplayMaster/localization/eng/cards.json)；删除无效的 `replay_master_ui.json`。
- **验证**：日志仍应出现对 **cards.json** 的 mod merge；英/中界面打出选牌无 `LocException`。
