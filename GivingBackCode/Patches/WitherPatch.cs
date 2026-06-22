using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 追踪每张 Wither 牌由沙漏累计获得的额外伤害加成。
/// </summary>
internal static class WitherBonusDamageTracker
{
    private static readonly ConditionalWeakTable<Wither, BonusState> Table = new();

    public static void AddBonus(Wither wither, decimal amount)
        => Table.GetOrCreateValue(wither).Amount += amount;

    public static decimal GetBonus(Wither wither)
        => Table.TryGetValue(wither, out var s) ? s.Amount : 0m;

    internal sealed class BonusState { public decimal Amount; }
}

/// <summary>
/// 修改 Wither 状态牌：
///   - 新增关键字：RETAIN（留存至下回合）、EXHAUST（触发后消耗）
///   - 新效果：回合结束时若在手牌中，受到 2+2N（苦难8层及以上：3+3N）点不可格挡伤害后 Exhaust
///     N = Aeonglass 对该 Wither 的升级次数（FakeUpgradeLevel）
/// </summary>
[HarmonyPatch(typeof(Wither), "OnTurnEndInHand")]
public static class WitherOnTurnEndPatch
{
    private static readonly FieldInfo FakeUpgradeLevelField =
        AccessTools.Field(typeof(Wither), "_fakeUpgradeLevel");

    static bool Prefix(Wither __instance, PlayerChoiceContext choiceContext, ref Task __result)
    {
        __result = NewOnTurnEndInHand(__instance, choiceContext);
        return false;
    }

    static async Task NewOnTurnEndInHand(Wither instance, PlayerChoiceContext context)
    {
        var n = (int)FakeUpgradeLevelField.GetValue(instance)!;
        var ascLevel = instance.Owner.RunState.AscensionLevel;

        // 2+2N（苦难8+为3+3N），N = FakeUpgradeLevel
        var baseDmg = ascLevel >= 8 ? 3m : 2m;
        var damage = baseDmg * (1m + n) + WitherBonusDamageTracker.GetBonus(instance);

        await CreatureCmd.Damage(context, instance.Owner.Creature, damage, ValueProp.Move, instance);
    }
}

/// <summary>
/// 给 Wither 添加 RETAIN 和 EXHAUST 关键字（用于 tooltip 显示）。
/// </summary>
[HarmonyPatch(typeof(Wither), "get_CanonicalKeywords")]
public static class WitherKeywordsPatch
{
    static void Postfix(ref IEnumerable<CardKeyword> __result)
    {
        __result = __result
            .Where(k => k != CardKeyword.Unplayable)
            .Append(CardKeyword.Retain);
    }
}

/// <summary>
/// 实现 Wither 的 RETAIN 效果：
/// _keywords 与 ShouldRetainThisTurn 是两套独立系统，
/// 必须直接 patch get_ShouldRetainThisTurn 才能让牌实际留在手里。
/// </summary>
[HarmonyPatch(typeof(CardModel), "get_ShouldRetainThisTurn")]
public static class WitherRetainPatch
{
    static void Postfix(CardModel __instance, ref bool __result)
    {
        if (__instance is Wither) __result = true;
    }
}

/// <summary>
/// 使 Wither 可以从手牌打出（原版作为状态牌 IsPlayable 返回 false）。
/// 打出时无额外效果——靠 EXHAUST 关键字由游戏框架自动送入废牌堆，
/// 从而让玩家可以主动花费能量消耗 Wither、跳过回合末伤害。
/// </summary>
[HarmonyPatch(typeof(CardModel), "get_IsPlayable")]
public static class WitherIsPlayablePatch
{
    static void Postfix(CardModel __instance, ref bool __result)
    {
        if (__instance is Wither) __result = true;
    }
}

/// <summary>
/// 修正 Wither 的 DynamicVars["Damage"] 显示值，使 tooltip 与实际公式（2+2N）一致。
/// Aeonglass 每次调用 MatchWitherToUpgradeCount 后同步更新。
/// </summary>
[HarmonyPatch(typeof(Aeonglass), "MatchWitherToUpgradeCount")]
public static class AeonglassWitherDamagePatch
{
    private static readonly FieldInfo FakeUpgradeLevelField =
        AccessTools.Field(typeof(Wither), "_fakeUpgradeLevel");

    static void Postfix(Wither wither)
    {
        var n = (int)FakeUpgradeLevelField.GetValue(wither)!;
        // 更新 tooltip 显示的基础伤害值为 2+2N（苦难加成在运行时计算，不在 tooltip 中展示）
        wither.DynamicVars["Damage"].BaseValue = 2m * (1m + n);
    }
}
