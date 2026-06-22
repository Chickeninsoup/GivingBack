using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 修改 Glow：
///   获得 1 颗星（升级后 2 颗），抽 2 张牌。
/// </summary>
[HarmonyPatch(typeof(Glow), "OnPlay")]
public static class GlowOnPlayPatch
{
    static bool Prefix(Glow __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext);
        return false;
    }

    static async Task NewOnPlay(Glow instance, PlayerChoiceContext choiceContext)
    {
        var player = instance.Owner!;
        var stars = instance.IsUpgraded ? 2m : 1m;

        await PlayerCmd.GainStars(stars, player);
        await CardPileCmd.Draw(choiceContext, player);
        await CardPileCmd.Draw(choiceContext, player);
    }
}

/// <summary>
/// 升级后仅标记 IsUpgraded，不做其他操作（star 数量由 OnPlay 根据 IsUpgraded 动态决定）。
/// </summary>
[HarmonyPatch(typeof(Glow), "OnUpgrade")]
public static class GlowOnUpgradePatch
{
    static bool Prefix(Glow __instance)
    {
        __instance.EnergyCost.FinalizeUpgrade();
        __instance.DynamicVars.Stars.BaseValue = 2m;
        return false;
    }
}
