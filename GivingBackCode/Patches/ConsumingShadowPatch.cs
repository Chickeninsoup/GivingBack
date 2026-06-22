using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 重做 Consuming Shadow：
///   注入 2 颗暗能法球。
///   在你的回合结束时，触发最左边和最右边法球的被动效果。
/// </summary>
[HarmonyPatch(typeof(ConsumingShadow), "OnPlay")]
public static class ConsumingShadowOnPlayPatch
{
    static bool Prefix(ConsumingShadow __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext);
        return false;
    }

    static async Task NewOnPlay(ConsumingShadow instance, PlayerChoiceContext choiceContext)
    {
        var player = instance.Owner!;
        await OrbCmd.Channel<DarkOrb>(choiceContext, player);
        await OrbCmd.Channel<DarkOrb>(choiceContext, player);

        // 应用 ConsumingShadowPower（以便回合结束效果触发）
        await PowerCmd.Apply<ConsumingShadowPower>(choiceContext, player.Creature, 1m, player.Creature, null, false);
    }
}

/// <summary>
/// 替换 ConsumingShadowPower 的回合结束效果：触发最左和最右法球的被动效果。
/// </summary>
[HarmonyPatch(typeof(ConsumingShadowPower), "AfterSideTurnEnd")]
public static class ConsumingShadowPowerPatch
{
    static bool Prefix(ConsumingShadowPower __instance, ref Task __result)
    {
        __result = NewAfterSideTurnEnd(__instance);
        return false;
    }

    static async Task NewAfterSideTurnEnd(ConsumingShadowPower instance)
    {
        var player = instance.Owner?.Player;
        if (player == null) return;

        var orbs = player.PlayerCombatState?.OrbQueue?.Orbs;
        if (orbs == null || orbs.Count == 0) return;

        var ctx = new ThrowingPlayerChoiceContext();
        OrbModel leftOrb = orbs[0];
        OrbModel rightOrb = orbs[orbs.Count - 1];

        await OrbCmd.Passive(ctx, leftOrb, null!);  // 最左边（DarkOrb 等不接受 creature target，传 null）
        if (orbs.Count > 1)
            await OrbCmd.Passive(ctx, rightOrb, null!);  // 最右边
    }
}
