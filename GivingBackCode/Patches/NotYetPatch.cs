using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 重做 Not Yet：
///   1 费 Skill，EXHAUST。
///   效果：消耗手牌中所有牌，每消耗一张摸 1 张牌。
///   升级后：0 费。
/// </summary>

/// <summary>
/// 替换 OnPlay：消耗所有手牌，每张消耗后摸 1 牌。
/// </summary>
[HarmonyPatch(typeof(NotYet), "OnPlay")]
public static class NotYetOnPlayPatch
{
    static bool Prefix(NotYet __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext);
        return false;
    }

    static async Task NewOnPlay(NotYet instance, PlayerChoiceContext choiceContext)
    {
        var player = instance.Owner!;
        // 拍摄快照：排除 Not Yet 本身（正在出牌，已不在手牌区）
        var handCards = CardPile.Get(PileType.Hand, player)!.Cards
            .Where(c => c != instance)
            .ToList();

        foreach (var card in handCards)
        {
            await CardCmd.Exhaust(choiceContext, card, false, false);
            await CardPileCmd.Draw(choiceContext, player);
        }
    }
}

/// <summary>
/// 替换 OnUpgrade：升级后费用改为 0。
/// </summary>
[HarmonyPatch(typeof(NotYet), "OnUpgrade")]
public static class NotYetOnUpgradePatch
{
    static bool Prefix(NotYet __instance)
    {
        __instance.EnergyCost.UpgradeBy(-1);
        __instance.EnergyCost.FinalizeUpgrade();
        return false;
    }
}
