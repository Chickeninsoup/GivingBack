using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 修改 Iteration：
///   每当你打出能力牌，摸 1 张牌。
///   升级后：0 费。
///
/// 实现参考 Subroutine（SubroutinePower.AfterCardPlayed 机制）：
///   - Iteration.OnPlay → Apply IterationPower
///   - IterationPower.AfterCardDrawn → 替换为 no-op（去除原版"摸牌时摸牌"效果）
///   - AbstractModel.AfterCardPlayed 中过滤 IterationPower 实例，
///     打出能力牌时触发摸 1 张牌。
/// </summary>
[HarmonyPatch(typeof(Iteration), "OnPlay")]
public static class IterationOnPlayPatch
{
    static bool Prefix(Iteration __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext);
        return false;
    }

    static async Task NewOnPlay(Iteration instance, PlayerChoiceContext choiceContext)
    {
        var player = instance.Owner!;
        await PowerCmd.Apply<IterationPower>(choiceContext, player.Creature, 1m, player.Creature, null, false);
    }
}

/// <summary>
/// 去除原版 IterationPower.AfterCardDrawn 效果（原版在摸牌时摸牌，新版不需要）。
/// </summary>
[HarmonyPatch(typeof(IterationPower), "AfterCardDrawn")]
public static class IterationPowerDrawSuppressPatch
{
    static bool Prefix(IterationPower __instance, ref Task __result)
    {
        __result = Task.CompletedTask;
        return false;
    }
}

/// <summary>
/// IterationPower 在任意能力牌打出时摸 1 张牌（同 SubroutinePower.AfterCardPlayed 机制）。
/// </summary>
[HarmonyPatch(typeof(AbstractModel), "AfterCardPlayed")]
public static class IterationAfterCardPlayedPatch
{
    static void Postfix(AbstractModel __instance, ref Task __result,
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (__instance is not IterationPower iterPower) return;
        if (cardPlay.Card.Type != CardType.Power) return;
        if (cardPlay.Card is Iteration) return;  // 避免 Iteration 自身触发

        var player = iterPower.Owner?.Player;
        if (player == null) return;

        var prev = __result;
        __result = DrawCard(prev, choiceContext, player);
    }

    static async Task DrawCard(Task prev, PlayerChoiceContext choiceContext, Player player)
    {
        await prev;
        await CardPileCmd.Draw(choiceContext, player);
    }
}

[HarmonyPatch]
public static class IterationUpgradePatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        // Iteration 没有自己的 OnUpgrade — 回退到基类
        yield return AccessTools.Method(typeof(Iteration), "OnUpgrade")
                  ?? AccessTools.Method(typeof(CardModel), "OnUpgrade");
    }

    static bool Prefix(CardModel __instance)
    {
        if (__instance is not Iteration) return true;
        var energyCost = __instance.EnergyCost;
        var energyCostType = energyCost.GetType();
        AccessTools.Field(energyCostType, "_base")?.SetValue(energyCost, 0);
        AccessTools.Field(energyCostType, "<Canonical>k__BackingField")?.SetValue(energyCost, 0);
        energyCost.FinalizeUpgrade();
        return false;
    }
}
