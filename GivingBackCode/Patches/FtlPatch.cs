using System.Reflection;
using System.Runtime.CompilerServices;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 修改 FTL：
///   Deal 5(6) damage.
///   If this is the first time you play this card this combat, draw 1 card.
/// </summary>

// ── 追踪每场战斗中 Ftl 是否已被打出过 ─────────────────────────────────────

internal static class FtlPlayedTracker
{
    // 全局 HashSet：记录本次战斗中已被打出的 Ftl 实例
    private static readonly HashSet<CardModel> PlayedThisCombat = new();

    public static bool HasPlayed(CardModel card) => PlayedThisCombat.Contains(card);
    public static void MarkPlayed(CardModel card) => PlayedThisCombat.Add(card);
    public static void ResetAll() => PlayedThisCombat.Clear();
}

// ── 战斗结束时重置追踪状态 ────────────────────────────────────────────────

[HarmonyPatch]
public static class FtlCombatResetPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        var m = AccessTools.Method(typeof(AbstractModel), "AfterCombatEnd");
        if (m != null) yield return m;
        else MainFile.Logger.Info("[FtlCombatResetPatch] AbstractModel.AfterCombatEnd not found; FTL draw bonus will only trigger once per run.");
    }

    static void Postfix()
    {
        FtlPlayedTracker.ResetAll();
    }
}

// ── 替换 Ftl OnPlay ───────────────────────────────────────────────────────

[HarmonyPatch(typeof(Ftl), "OnPlay")]
public static class FtlOnPlayPatch
{
    static bool Prefix(Ftl __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    static async Task NewOnPlay(Ftl instance, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 造成 Damage 点伤害（base 5，升级后 6）
        await CommonActions.CardAttack(instance, cardPlay).Execute(null);

        // 首次打出：摸 1 张牌
        if (!FtlPlayedTracker.HasPlayed(instance))
        {
            FtlPlayedTracker.MarkPlayed(instance);
            await CardPileCmd.Draw(choiceContext, instance.Owner!);
        }
    }
}

// ── 升级：Damage 固定为 6 ─────────────────────────────────────────────────

[HarmonyPatch(typeof(Ftl), "OnUpgrade")]
public static class FtlUpgradePatch
{
    [HarmonyPostfix]
    static void FixUpgradeValue(Ftl __instance)
    {
        __instance.DynamicVars["Damage"].BaseValue = 6m;
    }
}
