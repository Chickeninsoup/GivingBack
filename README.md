# GivingBack

> A **Slay the Spire 2** mod that rebalances vanilla cards and relic, and reworks the Aeonglass boss fight's Wither mechanic.

---

## 依赖 / Dependencies

在安装或构建本 mod 之前，请确认以下依赖已就绪：

| 依赖 | 说明 |
|---|---|
| **Slay the Spire 2** | Steam 正版游戏本体（Early Access）|
| **BaseLib** ≥ v3.0.0 | 由 [Alchyr](https://github.com/Alchyr/BaseLib-StS2) 提供的 STS2 mod 基础库，需在游戏内 mod 管理器中先行启用 |
| **.NET 9.0 SDK** | 仅**从源码构建**时需要，[下载地址](https://dotnet.microsoft.com/download/dotnet/9.0) |

---

## 安装方式 / Installation

### 方法一：直接使用预编译 DLL（推荐）

1. 从 [Releases](../../releases) 页面下载最新版本的压缩包。
2. 解压后将 `GivingBack/` 文件夹（含 `GivingBack.dll`、`GivingBack.json`、`GivingBack.pdb`）整体复制到游戏的 mods 目录：

   | 平台 | mods 目录路径 |
   |---|---|
   | **Windows** | `<Steam>\steamapps\common\Slay the Spire 2\mods\` |
   | **macOS** | `<Steam>/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/` |
   | **Linux** | `<Steam>/steamapps/common/Slay the Spire 2/mods/` |

3. 启动游戏，进入主菜单的 **Mods** 界面，确认 `BaseLib` 和 `GivingBack` 均已勾选启用。

---

### 方法二：从源码构建

**前提**：已安装 .NET 9.0 SDK，且 Steam 上已安装 Slay the Spire 2。

```bash
# 1. 克隆仓库
git clone https://github.com/<your-username>/GivingBack.git
cd GivingBack/GivingBack

# 2. 构建（自动检测游戏路径并复制 DLL 到 mods 文件夹）
dotnet build
```

构建成功后，`GivingBack.dll`、`GivingBack.json`、`GivingBack.pdb` 会自动复制到游戏的 `mods/GivingBack/` 目录。

> **注意**：若游戏未安装在默认 Steam 路径，可在 `GivingBack/Directory.Build.props` 中手动指定 `Sts2Path`：
> ```xml
> <PropertyGroup>
>   <Sts2Path>D:/Games/Slay the Spire 2</Sts2Path>
> </PropertyGroup>
> ```

---

## 内容说明 / What This Mod Changes

### 卡牌改动

#### 战士（Ironclad）

| 卡牌 | 改动 |
|---|---|
| **Untouchable（无懈可击）** | 格挡 6→9（升级后 9→11）；稀有度→Common |
| **Flame Barrier（火焰屏障）** | 格挡 12→16（升级后 16→20），反伤 4→6（升级后 6→8）|
| **Stone Armor（石甲）** | Plating 4→5（升级后增量不变）|
| **Spite（怨恨）** | 伤害 5→6（升级后 9），若本回合失去过生命则抽 1 张牌 |
| **Go for the Eyes（瞄准眼睛）** | 伤害 3→4（升级后 4→6）|
| **Blood Wall（血墙）** | 失去生命从 2 改为 1 |
| **Not Yet（时候未到）** | 重做：1（升级后 0）费 Skill，Exhaust，消耗所有手牌并各抽 1 张 |
| **Forgotten Ritual（遗忘仪式）** | 移除 Exhaust 关键字 |

#### 猎宝（Silent）

| 卡牌 | 改动 |
|---|---|
| **Acrobatics（杂技）** | 稀有度: Uncommon —> Common |
| **Untouchable (触不可及)** | 获得格挡: 6(9) -> 9(11) |



#### 机宝（Defect）

| 卡牌 | 改动 |
|---|---|
| **FTL** | 伤害 3→5（升级后 4→6）；首次打出时抽 1 张牌 |
| **Glacier（冰川）** | 格挡 6→9；注入 1 颗冰霜球（升级后 2 颗）|
| **Hailstorm（冰雹暴）** | 重做：1 费 Skill，对单体造成 6 + 3×（当前冰霜球数量）点伤害，实时显示；升级后每球 +4 点 |
| **Helix Drill（螺旋钻）** | 重做：对手牌/抽牌堆/弃牌堆中每张状态牌造成 3（升级后 5）点伤害；稀有度→Uncommon，移除 Retain |
| **Synchronize（同步）** | 每有一个充能球获得 1 点能量；移除原版 Exhaust；升级后获得 Retain |
| **Spinner（旋转器）** | 注入 1 颗玻璃法球；每回合开始注入 1 颗；升级后 0 费 |
| **Consuming Shadow（吞噬暗影）** | 生成 2 颗暗能法球；回合结束时触发最左和最右充能球被动效果 |
| **Coolant（冷却剂）** | 每当摸到状态牌，抽 1 张牌 |
| **Feral（野性）** | 2（升级后 1）费；获得 2 点专注，失去 1 个法球槽位 |
| **Production（生产）** | 升级后获得能量 2→2（移除 Exhaust）|
| **Iteration（迭代）** | 打出能力牌时抽 1 张牌；升级后 0 费 |
| **Smokestack（烟囱）** | 每回合首次摸到状态牌时抽 2 张牌 |
| **Trash to Treasure（变废为宝）** | 重做：4 费 Skill，Exhaust；将手牌/抽牌堆/弃牌堆中所有状态牌变形为 Fuel（升级后 Fuel+，并获得 Retain）|
| **Scavenge（内存清理）** | 消耗手牌中 1 张，获得等同于其费用的能量；升级后 1→0 费 |


#### 君君 (Regent)

| 卡牌 | 改动 |
|---|---|
| **Glow（发光）** | 获得 1（升级后 2）颗星，抽 2 张牌 |


#### 白卡

| 卡牌 | 改动 |
|---|---|
| **Calamity** | 重做为 "Not Yet"：1（升级后 0）费 Skill，Exhaust，消耗手牌中所有牌，每消耗一张抽 1 牌 |



---

#### 敌人相关修改：


### Aeonglass Boss 机制重做

Wither 牌的强化体系完全重做：

- **战斗开始**：向抽牌堆底部塞入 `⌊牌组大小 / 5⌋` 张 Wither （向下取整）
- **每打出 4 张牌**：战斗中所有 Wither 伤害 +3
- **强化回合（原版塞 Wither）→ 改为**：
  - 将弃牌堆和消耗堆中所有 Wither 随机洗回抽牌堆
  - 所有 Wither 伤害 +3
  - 保留原版的 力量加成等效果

###

---

### Infested Prism 行为重做

- **第3n回合**：移除了多段攻击
- **强化回合**：活力火花不在增加，改为获得6点力量
#### 遗物改动

| 遗物 | 改动 |
|---|---|
| **Silken Tress（丝滑发丝）** | 添加效果：首个卡牌奖励可刷新一次 |
|**Nutritious Oyster（美味蛤蜊）**| 提升生命上限11->22 |

---

### 其他

- 商人删牌价格改动：A10 删牌价格修改为 75+25n
- 地图生成（测试中）:
  问号出现普通战斗的概率增加到20%。
  每一层只会至多出现一次问号里面出现普通敌人

---

## 项目结构

```
GivingBack/
├── GivingBackCode/
│   ├── Cards/          # 自定义卡牌
│   ├── Extensions/     # 扩展方法
│   ├── Patches/        # 所有 Harmony patch
│   ├── Powers/         # 自定义能力
│   └── Relics/         # 自定义遗物
├── GivingBack.csproj
├── GivingBack.json     # mod 元数据
└── README.md
```

---

## 开发参考

- [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) — mod 基础框架
- [ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) — 官方 mod 模板及 wiki
- [HarmonyLib](https://github.com/pardeike/Harmony) — 运行时 patch 框架
