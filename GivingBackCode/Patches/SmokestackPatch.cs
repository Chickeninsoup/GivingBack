using System.Reflection;
using System.Runtime.CompilerServices;
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
/// 重做 Smokestack：
///   每回合首次摸到状态牌时，摸 2 张牌（升级后 3 张）。
///
/// 实现参考原版 Iteration（IterationPower.AfterCardDrawn 机制）：
///   - Smokestack.OnPlay → Apply SmokestackPower（携带摸牌数量为 stacks）
///   - AbstractModel.AfterCardDrawn 中过滤 SmokestackPower 实例，
///     每回合第一次摸到状态牌时触发。
/// </summary>

internal static class SmokestackStateTracker
{
    private static readonly ConditionalWeakTable<SmokestackPower, TurnState> Table = new();

    public static bool TryTrigger(SmokestackPower power)
    {
        var state = Table.GetOrCreateValue(power);
        if (state.TriggeredThisTurn) return false;
        state.TriggeredThisTurn = true;
        return true;
    }

    public static void ResetTurn(SmokestackPower power)
    {
        if (Table.TryGetValue(power, out var state))
            state.TriggeredThisTurn = false;
    }

    internal sealed class TurnState
    {
        public bool TriggeredThisTurn;
    }
}

/// <summary>
/// Smokestack 打出时：应用 SmokestackPower，stacks = 摸牌数（2 或 3）。
/// </summary>
[HarmonyPatch(typeof(Smokestack), "OnPlay")]
public static class SmokestackOnPlayPatch
{
    static bool Prefix(Smokestack __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext);
        return false;
    }

    static async Task NewOnPlay(Smokestack instance, PlayerChoiceContext choiceContext)
    {
        var player = instance.Owner!;
        decimal drawCount = instance.IsUpgraded ? 3m : 2m;
        await PowerCmd.Apply<SmokestackPower>(choiceContext, player.Creature, drawCount, player.Creature, null, false);
    }
}

/// <summary>
/// 替换 SmokestackPower 现有的 AfterCardGeneratedForCombat（如有）为无效，
/// 并在 AbstractModel.AfterCardDrawn 中添加新的摸牌触发逻辑。
/// </summary>
[HarmonyPatch(typeof(SmokestackPower), "AfterCardGeneratedForCombat")]
public static class SmokestackPowerSuppressOriginalPatch
{
    static bool Prefix(SmokestackPower __instance, ref Task __result)
    {
        __result = Task.CompletedTask;
        return false;
    }
}

/// <summary>
/// 每回合首次摸到状态牌时，让 SmokestackPower 触发摸 Stacks 张牌。
/// 同原版 IterationPower.AfterCardDrawn 机制。
/// </summary>
[HarmonyPatch(typeof(AbstractModel), "AfterCardDrawn")]
public static class SmokestackAfterCardDrawnPatch
{
    static void Postfix(AbstractModel __instance, ref Task __result, CardModel card, bool fromHandDraw)
    {
        if (__instance is not SmokestackPower smokePower) return;
        if (card.Type != CardType.Status) return;
        if (!SmokestackStateTracker.TryTrigger(smokePower)) return;

        var player = smokePower.Owner?.Player;
        if (player == null) return;

        var drawCount = smokePower.Amount;
        var prev = __result;
        __result = DrawCards(prev, player, drawCount);
    }

    static async Task DrawCards(Task prev, Player player, decimal count)
    {
        await prev;
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), count, player);
    }
}

/// <summary>
/// 每回合开始时重置 SmokestackPower 的回合触发状态。
/// </summary>
[HarmonyPatch(typeof(AbstractModel), "AfterPlayerTurnStart")]
public static class SmokestackTurnResetPatch
{
    static void Postfix(AbstractModel __instance, Player player)
    {
        if (__instance is not SmokestackPower smokePower) return;
        SmokestackStateTracker.ResetTurn(smokePower);
    }
}

[HarmonyPatch(typeof(Smokestack), "OnUpgrade")]
public static class SmokestackUpgradePatch
{
    static bool Prefix(Smokestack __instance)
    {
        __instance.EnergyCost.FinalizeUpgrade();
        return false;
    }
}
