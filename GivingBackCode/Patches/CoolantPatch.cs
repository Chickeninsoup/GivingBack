using System.Collections.Generic;
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
/// 追踪每个 CoolantPower 的"待额外摸牌数"与"正在额外摸牌"状态，
/// 防止额外摸牌时再次触发计数（无限循环）。
/// </summary>
internal static class CoolantStateTracker
{
    private static readonly ConditionalWeakTable<CoolantPower, DrawState> Table = new();

    public static void IncrementPending(CoolantPower power)
        => Table.GetOrCreateValue(power).Pending++;

    public static int DrainPending(CoolantPower power)
    {
        var state = Table.GetOrCreateValue(power);
        int count = state.Pending;
        state.Pending = 0;
        return count;
    }

    public static bool IsDrawing(CoolantPower power)
        => Table.TryGetValue(power, out var s) && s.Active;

    public static void SetDrawing(CoolantPower power, bool value)
        => Table.GetOrCreateValue(power).Active = value;

    internal sealed class DrawState
    {
        public bool Active;
        public int Pending;
    }
}

/// <summary>
/// 重做 Coolant：
///   2 费（升级后 1 费）。
///   每当你摸到一张状态牌，摸 1 张牌。
///
/// 批量触发模式：
///   初始摸牌链全部完成后，统计摸到了多少张状态牌，
///   然后一次性额外摸对应数量的牌。
///   额外摸牌期间不再计数，避免无限循环。
/// </summary>
[HarmonyPatch(typeof(Coolant), "OnPlay")]
public static class CoolantOnPlayPatch
{
    static bool Prefix(Coolant __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext);
        return false;
    }

    static async Task NewOnPlay(Coolant instance, PlayerChoiceContext choiceContext)
    {
        var player = instance.Owner!;
        await PowerCmd.Apply<CoolantPower>(choiceContext, player.Creature, 1m, player.Creature, null, false);
    }
}

/// <summary>
/// 去除 CoolantPower 原版的回合开始效果（替换为 no-op）。
/// </summary>
[HarmonyPatch(typeof(CoolantPower), "AfterSideTurnStart")]
public static class CoolantPowerSuppressOriginalPatch
{
    static bool Prefix(CoolantPower __instance, ref Task __result)
    {
        __result = Task.CompletedTask;
        return false;
    }
}

/// <summary>
/// 摸到状态牌时，累计计数（批量模式：不立即触发额外摸牌）。
/// 正在额外摸牌时跳过，防止递归。
/// </summary>
[HarmonyPatch(typeof(AbstractModel), "AfterCardDrawn")]
public static class CoolantAfterCardDrawnPatch
{
    static void Postfix(AbstractModel __instance, PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (__instance is not CoolantPower coolantPower) return;
        if (card.Type != CardType.Status) return;

        var player = coolantPower.Owner?.Player;
        if (player == null) return;

        if (CoolantStateTracker.IsDrawing(coolantPower)) return;

        CoolantStateTracker.IncrementPending(coolantPower);
    }
}

/// <summary>
/// CardPileCmd.Draw(ctx, player) 返回 Task&lt;CardModel&gt;。
/// 完成后批量触发 Coolant 的额外摸牌。
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), "Draw", new[] { typeof(PlayerChoiceContext), typeof(Player) })]
public static class CardPileCmdDrawSinglePatch
{
    static void Postfix(PlayerChoiceContext choiceContext, Player player, ref Task<CardModel> __result)
    {
        var coolantPower = player.Creature.Powers.OfType<CoolantPower>().FirstOrDefault();
        if (coolantPower == null) return;
        if (CoolantStateTracker.IsDrawing(coolantPower)) return;

        var prev = __result;
        __result = FireExtras(prev, coolantPower, choiceContext, player);
    }

    static async Task<CardModel> FireExtras(Task<CardModel> prev, CoolantPower coolantPower, PlayerChoiceContext choiceContext, Player player)
    {
        var result = await prev;
        int count = CoolantStateTracker.DrainPending(coolantPower);
        if (count > 0)
        {
            CoolantStateTracker.SetDrawing(coolantPower, true);
            try
            {
                for (int i = 0; i < count; i++)
                    await CardPileCmd.Draw(choiceContext, player);
            }
            finally
            {
                CoolantStateTracker.SetDrawing(coolantPower, false);
            }
        }
        return result;
    }
}

/// <summary>
/// CardPileCmd.Draw(ctx, decimal count, player, fromHandDraw) 返回 Task&lt;IEnumerable&lt;CardModel&gt;&gt;。
/// 完成后批量触发 Coolant 的额外摸牌。
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), "Draw", new[] { typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool) })]
public static class CardPileCmdDrawMultiPatch
{
    static void Postfix(PlayerChoiceContext choiceContext, Player player, ref Task<IEnumerable<CardModel>> __result)
    {
        var coolantPower = player.Creature.Powers.OfType<CoolantPower>().FirstOrDefault();
        if (coolantPower == null) return;
        if (CoolantStateTracker.IsDrawing(coolantPower)) return;

        var prev = __result;
        __result = FireExtras(prev, coolantPower, choiceContext, player);
    }

    static async Task<IEnumerable<CardModel>> FireExtras(Task<IEnumerable<CardModel>> prev, CoolantPower coolantPower, PlayerChoiceContext choiceContext, Player player)
    {
        var result = await prev;
        int count = CoolantStateTracker.DrainPending(coolantPower);
        if (count > 0)
        {
            CoolantStateTracker.SetDrawing(coolantPower, true);
            try
            {
                for (int i = 0; i < count; i++)
                    await CardPileCmd.Draw(choiceContext, player);
            }
            finally
            {
                CoolantStateTracker.SetDrawing(coolantPower, false);
            }
        }
        return result;
    }
}

[HarmonyPatch(typeof(Coolant), "OnUpgrade")]
public static class CoolantUpgradePatch
{
    static bool Prefix(Coolant __instance)
    {
        __instance.EnergyCost.UpgradeBy(-1);
        __instance.EnergyCost.FinalizeUpgrade();
        return false;
    }
}
