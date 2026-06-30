using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 修改原版卡牌和遗物的描述文本、名称。
/// 注入到游戏 LocTable 的内部字典中。
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public static class VanillaLocPatch
{
    private static readonly FieldInfo LocDictionaryField =
        AccessTools.Field(typeof(LocTable), "_translations");

    [HarmonyPostfix]
    static void ModifyVanillaDescriptions()
    {
        ModifyCardDescriptions();
        ModifyRelicDescriptions();
    }

    static void ModifyCardDescriptions()
    {
        var table = LocManager.Instance.GetTable("cards");
        var dict = (Dictionary<string, string>)LocDictionaryField.GetValue(table)!;

        // key 格式："{卡牌slug}.name" 或 "{卡牌slug}.description"
        // slug 通常是类名转为 snake_case（如 StrikeIronclad -> strike_ironclad）
        // 需要在游戏中确认，或查看 localization json 文件

        // 示例：
        // dict["strike_ironclad.name"] = "强力打击";
        // dict["strike_ironclad.description"] = "造成 {Damage} 点伤害。";
        // dict["defend_ironclad.name"] = "铁壁防御";
        // dict["defend_ironclad.description"] = "获得 {Block} 点格挡。";

        // 怨恨 (Spite): 更新描述为新版行为（摸牌替换多次攻击）
        dict["SPITE.description"] = "造成 {Damage:diff()} 点伤害；如果本回合失去过生命，抽1张牌。";

        // 枯萎 (Wither): RETAIN，回合末造成 2+3N 点不可格挡伤害（可主动出牌弃置）
        dict["WITHER.description"] = "回合结束时，若在手牌中，受到 {Damage} 点伤害。";

        // 血墙 (Blood Wall): 失去 HP 从 2 改为 1
        dict["BLOOD_WALL.description"] = "获得 {Block:diff()} 点格挡。失去1点生命值。";

        // Not Yet：新效果描述
        dict["NOT_YET.description"] = "消耗手牌中所有牌。每消耗一张牌，抽1张牌。";

        // Glow：获得 1 颗星（升级后 2 颗），抽 2 张牌
        dict["GLOW.description"] = "获得 {Stars:starIcons()}，抽2张牌。";

        // Calamity 重做为 Not Yet：治疗 10（升级后 13）点生命值，Exhaust
        dict["CALAMITY.name"] = "Heal";
        dict["CALAMITY.description"] = "回复 10(13) 点生命值。\n[gold]消耗[/gold]。";

        // FTL：Deal 5(6) damage. 首次打出摸 1 牌
        dict["FTL.description"] = "造成 {Damage:diff()}点伤害。\n若这是本场战斗首次打出此牌，抽1张牌。";

        // Hailstorm 重做：1 费 Skill，对敌人造成 6 + 3×冰霜球数量 伤害（实时显示）
        dict["HAILSTORM.description"] = "造成 {CalculatedDamage} 点伤害。\n(基础6，每颗[gold]冰霜[/gold]球额外 +{ExtraDamage:diff()} 点)";

        // Feral 重做：2(1) 费，获得 2 点专注，失去 1 个法球槽位
        dict["FERAL.description"] = "获得2点[gold]集中[/gold]。\n失去1个充能球栏位。";

        // Coolant 重做：摸到状态牌时摸 1 张牌
        dict["COOLANT.description"] = "每当你摸到一张状态牌，抽1张牌。";

        // Scavenge 重做：消耗手牌获得等值能量
        dict["SCAVENGE.description"] = "[gold]消耗[/gold]手牌中1张牌，获得等同于其费用的能量。";

        // Consuming Shadow 重做：注入 2 颗暗能法球，回合结束激活最左和最右法球
        dict["CONSUMING_SHADOW.description"] = "生成2颗[dark]暗黑[/dark]充能球。\n在你的回合结束时，激活你最左边和最右边的充能球的被动能力。";

        // Helix Drill 重做：对牌库每张状态牌造成 3 点伤害，保留
        dict["HELIX_DRILL.description"] = "对手牌、抽牌堆、弃牌堆中每张[gold]状态[/gold]牌造成 {Damage:diff()} 点伤害。";

        // Spinner 修改：注入玻璃法球，每回合开始时注入 1 颗，升级后 0 费
        dict["SPINNER.description"] = "生成1颗[gold]玻璃[/gold]充能球。\n在你的回合开始时，生成1颗[gold]玻璃[/gold]充能球。";

        // Synchronize 修改：每有一个充能球获得 1 点能量
        dict["SYNCHRONIZE.description"] = "每有一个[gold]充能球[/gold]，获得{Energy:energyIcons()}。";

        // Smokestack 重做：每回合首次摸到状态牌时摸 2(3) 张牌
        dict["SMOKESTACK.description"] = "每回合首次摸到[gold]状态[/gold]牌时，抽2张牌。";
        dict["SMOKESTACK+.description"] = "每回合首次摸到[gold]状态[/gold]牌时，抽3张牌。";

        // Hotfix 修改：移除消耗；升级后变为本回合获得 3 点集中
        dict["HOTFIX+.description"] = "本回合获得3点[gold]集中[/gold]。";

        // Iteration 修改：打出能力牌时摸 1 张牌，升级后 0 费
        dict["ITERATION.description"] = "每当你打出[gold]能力[/gold]牌，抽1张牌。";

        // Trash to Treasure 重做：3 费 Skill，Exhaust，将手牌/牌库/弃牌堆状态牌变形为 Fuel（升级后保留）
        dict["TRASH_TO_TREASURE.description"] = "将手牌、牌库和弃牌堆中所有[gold]状态[/gold]牌[gold]变化[/gold]为[gold]燃料[/gold]。";
        dict["TRASH_TO_TREASURE+.description"] = "将手牌、牌库和弃牌堆中所有[gold]状态[/gold]牌[gold]变化[/gold]为[gold]燃料+[/gold]。";

        // Consuming Shadow 修改：触发被动效果而非激活
        dict["CONSUMING_SHADOW.description"] = "[gold]生成[/gold]2颗[gold]黑暗[/gold]充能球。在你的回合结束时，触发最左边和最右边充能球的被动能力。";

        // Hand Trick 重做：弃1张手牌，获得1点能量（升级后获得保留）
        dict["HAND_TRICK.description"] = "丢弃手牌中1张牌，获得{Energy:energyIcons()}。";
        dict["HAND_TRICK+.description"] = "丢弃手牌中1张牌，获得{Energy:energyIcons()}。";

        // Calculated Gamble 修改：覆盖 base key（中文原文内嵌了"消耗。"文本，英文版无此行）
        // 去掉描述里的"消耗。"——Exhaust 由关键字系统另行显示，升级后由 patch 移除
        dict["CALCULATED_GAMBLE.description"] = "丢弃你的[gold]手牌[/gold]，\n然后抽同等数量的牌。";
        dict["CALCULATED_GAMBLE+.description"] = "丢弃你的[gold]手牌[/gold]，\n然后抽同等数量的牌。";

        // ── 在此添加你的卡牌描述修改 ────────────────────────────
    }

    static void ModifyRelicDescriptions()
    {
        var table = LocManager.Instance.GetTable("relics");
        var dict = (Dictionary<string, string>)LocDictionaryField.GetValue(table)!;

        // key 格式："{遗物slug}.title"、"{遗物slug}.description"、"{遗物slug}.flavor"
        // slug 通常是类名转为 snake_case（如 BurningBlood -> burning_blood）

        // 示例：
        // dict["burning_blood.description"] = "每场战斗结束后回复 {Magic} 点生命值。";
        // dict["burning_blood.flavor"] = "烈火淬炼，永不屈服。";

        // 丝滑发丝 (Silken Tress): 添加首个卡牌奖励可刷新一次
        dict["SILKEN_TRESS.description"] = "获得时，失去所有[gold]金币[/gold]。[gold]附魔[/gold]首个卡牌奖励中的所有卡牌（[purple]华彩[/purple]）。\n首个卡牌奖励可以刷新一次。";

        // ── 在此添加你的遗物描述修改 ────────────────────────────
    }
}
