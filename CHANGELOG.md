# Changelog

## [v0.2.3] - 2026-06-30

### 修复 / Fixed

- **Synchronize（同步）**：修复未升级版本显示消耗词条的问题（`get_CanonicalKeywords` 硬编码导致）
- **Hotfix**：移除原版 Exhaust 关键字

### 改动 / Changed

- **Hotfix** 升级效果重做：升级后效果变为本回合获得 3 点[gold]集中[/gold]（`TemporaryFocusPower`）
- **Smokestack+** 描述补全：升级后每回合首次摸到状态牌时抽 3 张牌（原逻辑已支持，补全显示文本）
- **Glacier** 重做已移除，恢复原版行为

---

## [v0.2.2] - 2026-06-30

### 修复 / Fixed

- **Aeonglass Wither 机制重做**：
  - 战斗开始钩子从 `BeforeCombatStart` 改为 `BeforeCombatStartLate`，修正动画时序问题
  - Wither 加入抽牌堆底后，现在使用游戏种子构造的确定性 RNG 将其随机插入整个牌库，而非固定堆底
  - 移除了原来每 3 回合将弃牌堆 Wither 洗回抽牌堆的逻辑（`AeonglassDiscardShufflePatch`）

- **Calamity**：修复打出后未进消耗堆的问题（新增 `GetResultPileTypeForCardPlay` Postfix，强制返回 `PileType.Exhaust`）

- **Hand Trick（手上技法）**：修复手牌为空时仍弹出选牌界面的问题；现在手牌非空才会触发选牌

- **Iteration（迭代）**：将触发钩子从 `AbstractModel.AfterCardPlayed` 改为 `Hook.AfterCardPlayed`，确保回响形态（Echo Form）重打能力牌时也能正确摸牌

- **MapGenerationPatch**：将参数注入改为 `RunManager.Instance?.State` 直接访问，修复注入失败导致补丁不生效的问题

### 新增 / Added

- **SubroutinePower 回响兼容**（`SubroutinePowerEchoFormPatch`）：使 Subroutine 在 Echo Form 重打技能牌时也能正常给费

---

## [v0.2.1] - 2026-06-24

### 修复 / Fixed

- **Calculated Gamble（计算下注）升级版**：修复升级时未能同时移除 `_keywords` 中 Exhaust 词条的问题

---

## [v0.2.0] - 2026-06-24

### 新增 / Added

- **Hand Trick（手上技法）重做**：效果全面重做
  - 费用改为 0
  - 新效果：弃手牌中 1 张牌，获得 1 点能量
  - 升级后：额外获得 Retain（保留）关键字

- **Calculated Gamble（计算下注）修改**：
  - 费用改为 1
  - 升级后：去除消耗词条，而不是原来的添加保留词条

- **Production（生产制造）修改**：
  - 升级后：去除消耗，而不是获得3费

### 改动 / Changed

- **Forgotten Ritual（遗忘仪式）升级版**：
  - 升级后移除 Exhaust（消耗）关键字
  - 升级后额外费用从 +4 改为 +3（即升级后花费 3 费而非 4 费）

- **描述文字修正**：
  - Synchronize、Hand Trick 的描述现在正确显示能量水晶图标 UI（`{Energy:energyIcons()}`）
  - Calamity 名称修正为 "Heal"
  - NOT_YET 描述移除多余的消耗标签文本
  - FTL 描述格式修正
  - Silken Tress 遗物描述用词修正（华彩）

---

## [v0.1.0] - 2026-06-24

### 初始版本 / Initial Release

#### 卡牌改动

**战士（Ironclad）**
- Untouchable（无懈可击）：格挡 6→9（升级后 9→11）；稀有度→Common
- Flame Barrier（火焰屏障）：格挡 12→16（升级后 16→20），反伤 4→6（升级后 6→8）
- Stone Armor（石甲）：Plating 4→5
- Spite（怨恨）：伤害 5→6（升级后 9），若本回合失去过生命则抽 1 张牌
- Go for the Eyes（瞄准眼睛）：伤害 3→4（升级后 4→6）
- Blood Wall（血墙）：失去生命从 2 改为 1
- Not Yet（时候未到）：重做为 1（升级后 0）费 Skill，Exhaust，消耗所有手牌并各抽 1 张

**猎宝（Silent）**
- Acrobatics（杂技）：稀有度 Uncommon→Common
- Untouchable（触不可及）：格挡 6(9)→9(11)

**机宝（Defect）**
- FTL：伤害 3→5（升级后 4→6）；首次打出时抽 1 张牌
- Glacier（冰川）：格挡 6→9；注入 1 颗冰霜球（升级后 2 颗）
- Hailstorm（冰雹暴）：重做为 1 费 Skill，造成 6 + 3×冰霜球数量点伤害
- Helix Drill（螺旋钻）：重做，对手牌/抽牌堆/弃牌堆中每张状态牌造成 3（升级后 5）点伤害；稀有度→Uncommon
- Synchronize（同步）：每有一个充能球获得 1 点能量；移除 Exhaust；升级后获得 Retain
- Spinner（旋转器）：注入 1 颗玻璃法球；每回合开始注入 1 颗；升级后 0 费
- Consuming Shadow（吞噬暗影）：生成 2 颗暗能法球；回合结束时触发最左和最右充能球被动
- Coolant（冷却剂）：每当摸到状态牌，抽 1 张牌
- Feral（野性）：2（升级后 1）费；获得 2 点专注，失去 1 个法球槽位
- Iteration（迭代）：打出能力牌时抽 1 张牌；升级后 0 费
- Smokestack（烟囱）：每回合首次摸到状态牌时抽 2 张牌
- Trash to Treasure（变废为宝）：重做为 4 费 Skill，Exhaust；将所有状态牌变形为 Fuel
- Scavenge（内存清理）：消耗手牌中 1 张，获得等同于其费用的能量；升级后 0 费

**君君（Regent）**
- Glow（发光）：获得 1（升级后 2）颗星，抽 2 张牌

**通用**
- Calamity：重做为治疗牌，回复 10（升级后 13）点生命值，Exhaust

#### 遗物改动
- Silken Tress（丝滑发丝）：首个卡牌奖励可刷新一次
- Nutritious Oyster（美味蛤蜊）：提升生命上限 11→22

#### 敌人改动
- Aeonglass Boss Wither 机制重做
- Infested Prism 行为重做

#### 其他
- 商人删牌价格：A10 改为 75+25n
- 地图生成：问号出现普通战斗概率增加到 20%，每层最多出现一次
