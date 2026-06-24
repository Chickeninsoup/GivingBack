using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 重做 Hand Trick（幻手）：
///   弃手牌中 1 张牌，获得 1 点能量（升级后获得 2 点能量）。
/// </summary>
[HarmonyPatch(typeof(HandTrick), "OnPlay")]
public static class HandTrickOnPlayPatch
{
    static bool Prefix(HandTrick __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext);
        return false;
    }

    static async Task NewOnPlay(HandTrick instance, PlayerChoiceContext choiceContext)
    {
        var player = instance.Owner!;

        var card = await CommonActions.SelectSingleCard(
            instance,
            new LocString("cards", "HAND_TRICK"),
            choiceContext,
            PileType.Hand);
        if (card == null) return;

        await CardCmd.Discard(choiceContext, card);

        await PlayerCmd.GainEnergy(1m, player);
    }
}

/// <summary>
/// 为 HandTrick 添加 EnergyVar(1)，使描述中的 {Energy:energyIcons()} 能正确渲染。
/// </summary>
[HarmonyPatch(typeof(HandTrick), "get_CanonicalVars")]
public static class HandTrickCanonicalVarsPatch
{
    [HarmonyPostfix]
    static void AddEnergyVar(ref IEnumerable<DynamicVar> __result)
    {
        __result = __result.Append(new EnergyVar(1));
    }
}

/// <summary>
/// 升级：跳过原版 Block DynamicVar 升级，改为添加 Retain 关键字。
/// </summary>
[HarmonyPatch(typeof(HandTrick), "OnUpgrade")]
public static class HandTrickUpgradePatch
{
    static bool Prefix(HandTrick __instance)
    {
        var kw = AccessTools.Field(typeof(CardModel), "_keywords")?.GetValue(__instance) as HashSet<CardKeyword>;
        kw?.Add(CardKeyword.Retain);
        return false;
    }
}
