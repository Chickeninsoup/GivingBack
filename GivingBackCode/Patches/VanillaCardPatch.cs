using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.ValueProps;

namespace GivingBack.GivingBackCode.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public static class VanillaCardPatch
{
    // CardModel 上的稀有度 backing field 实际叫 <Rarity>k__BackingField
    private static readonly FieldInfo CardRarityField =
        AccessTools.Field(typeof(CardModel), "<Rarity>k__BackingField");

    // CardModel 上的卡牌类型 backing field
    private static readonly FieldInfo CardTypeField =
        AccessTools.Field(typeof(CardModel), "<Type>k__BackingField");

    [HarmonyPostfix]
    static void ModifyVanillaCards()
    {
        if (CardRarityField == null)
        {
            MainFile.Logger.Error("CardRarityField not found! Field name may have changed.");
            return;
        }

        var rarityType = CardRarityField.FieldType;
        var common = Enum.Parse(rarityType, "Common");

        // 杂技 (Acrobatics) 稀有度 -> Common
        var acrobatics = ModelDb.AllCards.OfType<Acrobatics>().FirstOrDefault();
        if (acrobatics != null)
            CardRarityField.SetValue(acrobatics, common);

        // 无懈可击 (Untouchable) 防御 6->9（升级后 9->11）
        var untouchable = ModelDb.AllCards.OfType<Untouchable>().FirstOrDefault();
        if (untouchable != null)
            untouchable.DynamicVars["Block"].BaseValue = 9m;

        // 火焰屏障 (Flame Barrier) Block 12->16（升级 16->20），DamageBack 4->6（升级 6->8）
        var flameBarrier = ModelDb.AllCards.OfType<FlameBarrier>().FirstOrDefault();
        if (flameBarrier != null)
        {
            flameBarrier.DynamicVars["Block"].BaseValue = 16m;
            flameBarrier.DynamicVars["DamageBack"].BaseValue = 4m;
        }

        // 石甲 (Stone Armor)：Plating 4->5（升级后 6->7，升级增量不变）
        var stoneArmor = ModelDb.AllCards.OfType<StoneArmor>().FirstOrDefault();
        if (stoneArmor != null)
        {
            if (stoneArmor.DynamicVars.ContainsKey("PlatingPower"))
                stoneArmor.DynamicVars["PlatingPower"].BaseValue = 5m;
            else
                MainFile.Logger.Error($"StoneArmor DynamicVars keys: {string.Join(", ", stoneArmor.DynamicVars.Keys)}");
        }

        // 枯萎 (Wither)：CardType 保持 Status，移除 CardKeyword.Unplayable，费用改为 1
        // CardKeyword.Unplayable 双重拦截需移除，使其可主动打出
        var wither = ModelDb.AllCards.OfType<Wither>().FirstOrDefault();
        if (wither != null)
        {
            CardTypeField.SetValue(wither, CardType.Status);
            var witherKeywordsField = AccessTools.Field(typeof(CardModel), "_keywords");
            var witherKeywords = witherKeywordsField?.GetValue(wither) as HashSet<CardKeyword>;
            witherKeywords?.Remove(CardKeyword.Unplayable);

            var witherEnergyCost = wither.EnergyCost;
            var witherEnergyCostType = witherEnergyCost.GetType();
            AccessTools.Field(witherEnergyCostType, "_base")?.SetValue(witherEnergyCost, 1);
            AccessTools.Field(witherEnergyCostType, "<Canonical>k__BackingField")?.SetValue(witherEnergyCost, 1);
        }

        // 怨恨 (Spite) Damage 5->6（升级后 9），OnPlay 行为由 SpiteOnPlayPatch 替换
        var spite = ModelDb.AllCards.OfType<Spite>().FirstOrDefault();
        if (spite != null)
            spite.DynamicVars["Damage"].BaseValue = 6m;

        // 瞄准眼睛 (Go for the Eyes) Damage 3->4（升级后 4->6）
        var goForTheEyes = ModelDb.AllCards.OfType<GoForTheEyes>().FirstOrDefault();
        if (goForTheEyes != null)
            goForTheEyes.DynamicVars["Damage"].BaseValue = 4m;

        // FTL Damage 3->5（升级后 4->6）；首次打出摸牌由 FtlOnPlayPatch 处理
        var ftl = ModelDb.AllCards.OfType<Ftl>().FirstOrDefault();
        if (ftl != null)
            ftl.DynamicVars["Damage"].BaseValue = 5m;

        // Calamity 重做为 Not Yet：Type -> Skill，添加 Exhaust，费用改为 1
        var calamity = ModelDb.AllCards.OfType<Calamity>().FirstOrDefault();
        if (calamity != null)
        {
            CardTypeField.SetValue(calamity, CardType.Skill);
            var calamityKeywordsField = AccessTools.Field(typeof(CardModel), "_keywords");
            var calamityKeywords = calamityKeywordsField?.GetValue(calamity) as HashSet<CardKeyword>;
            calamityKeywords?.Add(CardKeyword.Exhaust);
            var calamityEnergyCost = calamity.EnergyCost;
            var calamityEnergyCostType = calamityEnergyCost.GetType();
            AccessTools.Field(calamityEnergyCostType, "_base")?.SetValue(calamityEnergyCost, 1);
            AccessTools.Field(calamityEnergyCostType, "<Canonical>k__BackingField")?.SetValue(calamityEnergyCost, 1);
        }

        // Not Yet：改为 1 费 Skill，EXHAUST；OnPlay 由 NotYetOnPlayPatch 替换
        var notYet = ModelDb.AllCards.OfType<NotYet>().FirstOrDefault();
        if (notYet != null)
        {
            // 直接操作 backing field 绕过 AssertMutable() 检查
            var energyCost = notYet.EnergyCost;
            var energyCostType = energyCost.GetType();
            AccessTools.Field(energyCostType, "_base")?.SetValue(energyCost, 1);
            AccessTools.Field(energyCostType, "<Canonical>k__BackingField")?.SetValue(energyCost, 1);

            CardTypeField.SetValue(notYet, CardType.Skill);

            var keywordsField = AccessTools.Field(typeof(CardModel), "_keywords");
            var keywords = keywordsField?.GetValue(notYet) as HashSet<CardKeyword>;
            keywords?.Add(CardKeyword.Exhaust);
        }

        // Hailstorm 重做：1 费 Skill，AnyEnemy，伤害 = 6 + 3×当前冰霜球数量
        // 实时伤害显示参考 AshenStrike（CalculatedDamageVar），计数参考 Voltalic（OrbQueue.Orbs）
        var hailstorm = ModelDb.AllCards.OfType<Hailstorm>().FirstOrDefault();
        if (hailstorm != null)
        {
            CardTypeField.SetValue(hailstorm, CardType.Skill);

            // TargetType: Self → AnyEnemy
            AccessTools.Field(typeof(CardModel), "<TargetType>k__BackingField")
                ?.SetValue(hailstorm, TargetType.AnyEnemy);

            // 费用改为 1
            var hailstormCost = hailstorm.EnergyCost;
            var hailstormCostType = hailstormCost.GetType();
            AccessTools.Field(hailstormCostType, "_base")?.SetValue(hailstormCost, 1);
            AccessTools.Field(hailstormCostType, "<Canonical>k__BackingField")?.SetValue(hailstormCost, 1);

            // 替换 DynamicVars：CalculationBase(6) + ExtraDamage(3) + CalculatedDamage(×FrostOrb数)
            var calcBase = new CalculationBaseVar(6m);
            var extraDmg = new ExtraDamageVar(3m);
            var calcDmg = new CalculatedDamageVar(ValueProp.Move)
                .WithMultiplier((CardModel card, Creature? _) =>
                    card.Owner?.PlayerCombatState?.OrbQueue.Orbs.Count(o => o is FrostOrb) ?? 0);

            // 设置 _owner，使 CalculatedDamageVar 能正确访问 CalculationBase / ExtraDamage
            var ownerField = AccessTools.Field(calcDmg.GetType(), "_owner");
            ownerField?.SetValue(calcBase, hailstorm);
            ownerField?.SetValue(extraDmg, hailstorm);
            ownerField?.SetValue(calcDmg, hailstorm);

            var hailstormVars = AccessTools.Field(hailstorm.DynamicVars.GetType(), "_vars")
                                ?.GetValue(hailstorm.DynamicVars) as Dictionary<string, DynamicVar>;
            if (hailstormVars != null)
            {
                hailstormVars.Remove("HailstormPower");
                hailstormVars["CalculationBase"] = calcBase;
                hailstormVars["ExtraDamage"] = extraDmg;
                hailstormVars["CalculatedDamage"] = calcDmg;
            }
        }

        // Feral 重做：2 费（升级后 1 费）；效果由 FeralPatch 替换
        var feral = ModelDb.AllCards.OfType<Feral>().FirstOrDefault();
        if (feral != null)
        {
            var feralCost = feral.EnergyCost;
            var feralCostType = feralCost.GetType();
            AccessTools.Field(feralCostType, "_base")?.SetValue(feralCost, 2);
            AccessTools.Field(feralCostType, "<Canonical>k__BackingField")?.SetValue(feralCost, 2);
        }

        // HelixDrill：稀有度 Rare → Uncommon
        var helixDrill = ModelDb.AllCards.OfType<HelixDrill>().FirstOrDefault();
        if (helixDrill != null)
        {
            var uncommon = Enum.Parse(rarityType, "Uncommon");
            CardRarityField.SetValue(helixDrill, uncommon);
        }

        // Hotfix：移除原版 Exhaust 关键字（升级后效果由 HotfixUpgradedOnPlayPatch 替换）
        var hotfix = ModelDb.AllCards.OfType<Hotfix>().FirstOrDefault();
        if (hotfix != null)
        {
            var hotfixKw = AccessTools.Field(typeof(CardModel), "_keywords")?.GetValue(hotfix) as HashSet<CardKeyword>;
            hotfixKw?.Remove(CardKeyword.Exhaust);
        }

        // Synchronize：移除原版 Exhaust 关键字（升级后添加 Retain，由 SynchronizeUpgradePatch 处理）
        var synchronize = ModelDb.AllCards.OfType<Synchronize>().FirstOrDefault();
        if (synchronize != null)
        {
            var syncKw = AccessTools.Field(typeof(CardModel), "_keywords")?.GetValue(synchronize) as HashSet<CardKeyword>;
            syncKw?.Remove(CardKeyword.Exhaust);
        }

        // Hand Trick（幻手）：费用改为 0
        var handTrick = ModelDb.AllCards.OfType<HandTrick>().FirstOrDefault();
        if (handTrick != null)
        {
            var htCost = handTrick.EnergyCost;
            var htCostType = htCost.GetType();
            AccessTools.Field(htCostType, "_base")?.SetValue(htCost, 0);
            AccessTools.Field(htCostType, "<Canonical>k__BackingField")?.SetValue(htCost, 0);
        }

        // Calculated Gamble：费用 0→1
        var calculatedGamble = ModelDb.AllCards.OfType<CalculatedGamble>().FirstOrDefault();
        if (calculatedGamble != null)
        {
            var cgCost = calculatedGamble.EnergyCost;
            var cgCostType = cgCost.GetType();
            AccessTools.Field(cgCostType, "_base")?.SetValue(cgCost, 1);
            AccessTools.Field(cgCostType, "<Canonical>k__BackingField")?.SetValue(cgCost, 1);
        }

        // Trash to Treasure 重做：3 费 Skill，添加 Exhaust 关键字
        var trashToTreasure = ModelDb.AllCards.OfType<TrashToTreasure>().FirstOrDefault();
        if (trashToTreasure != null)
        {
            CardTypeField.SetValue(trashToTreasure, CardType.Skill);
            var tttKw = AccessTools.Field(typeof(CardModel), "_keywords")?.GetValue(trashToTreasure) as HashSet<CardKeyword>;
            tttKw?.Add(CardKeyword.Exhaust);
            var tttCost = trashToTreasure.EnergyCost;
            var tttCostType = tttCost.GetType();
            AccessTools.Field(tttCostType, "_base")?.SetValue(tttCost, 4);
            AccessTools.Field(tttCostType, "<Canonical>k__BackingField")?.SetValue(tttCost, 4);
        }
    }
}

// 单独 patch OnUpgrade：原版升级会 UpgradeValueBy(3) 使 Block 变 12，修正为 11
[HarmonyPatch(typeof(Untouchable), "OnUpgrade")]
public static class UntouchableUpgradePatch
{
    [HarmonyPostfix]
    static void FixUpgradeValue(Untouchable __instance)
    {
        __instance.DynamicVars["Block"].BaseValue = 11m;
    }
}

// 怨恨 (Spite) 升级：原版升级 Repeat var，我们改为将 Damage 固定为 9
[HarmonyPatch(typeof(Spite), "OnUpgrade")]
public static class SpiteUpgradePatch
{
    [HarmonyPostfix]
    static void FixUpgradeValue(Spite __instance)
    {
        __instance.DynamicVars["Damage"].BaseValue = 9m;
    }
}

// 瞄准眼睛 (Go for the Eyes) 升级：原版升级后 Damage 4（我们改为 6）
[HarmonyPatch(typeof(GoForTheEyes), "OnUpgrade")]
public static class GoForTheEyesUpgradePatch
{
    [HarmonyPostfix]
    static void FixUpgradeValue(GoForTheEyes __instance)
    {
        __instance.DynamicVars["Damage"].BaseValue = 6m;
    }
}

// Production 升级：原版升级后 gain 3 mana + Exhaust，改为 gain 2 mana 且去除 Exhaust
[HarmonyPatch(typeof(Production), "OnUpgrade")]
public static class ProductionUpgradePatch
{
    [HarmonyPostfix]
    static void FixUpgradedProduction(Production __instance)
    {
        __instance.DynamicVars["Energy"].BaseValue = 2m;

        var keywordsField = AccessTools.Field(typeof(CardModel), "_keywords");
        var keywords = keywordsField?.GetValue(__instance) as HashSet<CardKeyword>;
        keywords?.Remove(CardKeyword.Exhaust);
    }
}

/// <summary>
/// Synchronize.get_CanonicalKeywords 可能硬编码了 Exhaust（同 ForgottenRitual 模式）。
/// 始终过滤掉 Exhaust，确保未升级版本卡面不显示消耗标签。
/// </summary>
[HarmonyPatch(typeof(Synchronize), "get_CanonicalKeywords")]
public static class SynchronizeCanonicalKeywordsPatch
{
    [HarmonyPostfix]
    static void RemoveExhaust(ref IEnumerable<CardKeyword> __result)
    {
        __result = __result.Where(k => k != CardKeyword.Exhaust);
    }
}

// Synchronize 升级：添加 Retain 关键字
[HarmonyPatch(typeof(Synchronize), "OnUpgrade")]
public static class SynchronizeUpgradePatch
{
    [HarmonyPostfix]
    static void AddRetain(Synchronize __instance)
    {
        var kw = AccessTools.Field(typeof(CardModel), "_keywords")?.GetValue(__instance) as HashSet<CardKeyword>;
        kw?.Add(CardKeyword.Retain);
    }
}

/// <summary>
/// 遗忘仪式 (ForgottenRitual) 升级：
///   - 原版 OnUpgrade 会调用 Energy.UpgradeValueBy(1)，即 Energy.BaseValue += 1（3→4）
///   - Postfix 将其反向修正回 3，让升级后 Energy 保持不变
///   - 同时从 _keywords 移除 Exhaust（控制行为：不再消耗）
/// </summary>
[HarmonyPatch(typeof(ForgottenRitual), "OnUpgrade")]
public static class ForgottenRitualUpgradePatch
{
    [HarmonyPostfix]
    static void FixUpgrade(ForgottenRitual __instance)
    {
        // 反向修正：原版 UpgradeValueBy(1) == BaseValue += 1，还原回 3
        __instance.DynamicVars["Energy"].BaseValue -= 1m;

        // 从 _keywords 移除 Exhaust（防止消耗行为触发）
        var kw = AccessTools.Field(typeof(CardModel), "_keywords")?.GetValue(__instance) as HashSet<CardKeyword>;
        kw?.Remove(CardKeyword.Exhaust);
    }
}

/// <summary>
/// 遗忘仪式升级后：CanonicalKeywords 过滤掉 Exhaust，使卡面不再显示消耗标签。
/// （ForgottenRitual.get_CanonicalKeywords 直接 newobj 返回 Exhaust，不经过 _keywords，须单独 patch）
/// </summary>
[HarmonyPatch(typeof(ForgottenRitual), "get_CanonicalKeywords")]
public static class ForgottenRitualCanonicalKeywordsPatch
{
    [HarmonyPostfix]
    static void RemoveExhaustWhenUpgraded(ForgottenRitual __instance, ref IEnumerable<CardKeyword> __result)
    {
        if (__instance.IsUpgraded)
            __result = __result.Where(k => k != CardKeyword.Exhaust);
    }
}

/// <summary>
/// Calculated Gamble 升级：
///   原版 OnUpgrade 调用 AddKeyword(Retain)，Postfix 将其撤销。
///   get_CanonicalKeywords 硬编码 Exhaust，由下方 patch 在升级后过滤。
/// </summary>
[HarmonyPatch(typeof(CalculatedGamble), "OnUpgrade")]
public static class CalculatedGambleUpgradePatch
{
    [HarmonyPostfix]
    static void RemoveRetainAndExhaust(CalculatedGamble __instance)
    {
        var kw = AccessTools.Field(typeof(CardModel), "_keywords")?.GetValue(__instance) as HashSet<CardKeyword>;
        kw?.Remove(CardKeyword.Retain);
        kw?.Remove(CardKeyword.Exhaust);
    }
}

[HarmonyPatch(typeof(CalculatedGamble), "get_CanonicalKeywords")]
public static class CalculatedGambleCanonicalKeywordsPatch
{
    [HarmonyPostfix]
    static void RemoveExhaustWhenUpgraded(CalculatedGamble __instance, ref IEnumerable<CardKeyword> __result)
    {
        if (__instance.IsUpgraded)
            __result = __result.Where(k => k != CardKeyword.Exhaust);
    }
}

// Trash to Treasure 升级后：ShouldRetainThisTurn = true（使 Retain 关键字实际生效）
[HarmonyPatch(typeof(CardModel), "get_ShouldRetainThisTurn")]
public static class TrashToTreasureRetainPatch
{
    static void Postfix(CardModel __instance, ref bool __result)
    {
        if (__instance is TrashToTreasure ttt && ttt.IsUpgraded) __result = true;
    }
}

