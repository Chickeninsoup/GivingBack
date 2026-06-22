using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 重做 Scavenge：
///   消耗手牌中 1 张牌，获得等同于其费用的能量。
/// </summary>
[HarmonyPatch(typeof(Scavenge), "OnPlay")]
public static class ScavengeOnPlayPatch
{
    static bool Prefix(Scavenge __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext);
        return false;
    }

    static async Task NewOnPlay(Scavenge instance, PlayerChoiceContext choiceContext)
    {
        var player = instance.Owner!;

        // 从手牌中选择 1 张牌（排除 Scavenge 自身）
        var card = await CommonActions.SelectSingleCard(instance, new LocString("cards", "SCAVENGE"), choiceContext, PileType.Hand);
        if (card == null) return;

        // 获得等同于该牌费用的能量
        var cost = card.EnergyCost.Canonical;
        await CardCmd.Exhaust(choiceContext, card, false, false);
        if (cost > 0)
            await PlayerCmd.GainEnergy((decimal)cost, player);
    }
}

/// <summary>
/// 升级后费用改为 0（原版升级 Energy +1，不需要）。
/// </summary>
[HarmonyPatch(typeof(Scavenge), "OnUpgrade")]
public static class ScavengeUpgradePatch
{
    static bool Prefix(Scavenge __instance)
    {
        __instance.EnergyCost.UpgradeBy(-1);
        __instance.EnergyCost.FinalizeUpgrade();
        return false;
    }
}
