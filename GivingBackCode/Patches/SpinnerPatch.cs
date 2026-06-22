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
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 修改 Spinner：
///   注入 1 颗玻璃法球。在你的回合开始时（能量重置时），注入 1 颗玻璃法球。
///   升级后：0 费。
///
/// 实现：
///   - Spinner.OnPlay 替换为：Channel 1 GlassOrb + Apply SpinnerPower
///   - SpinnerPower.AfterEnergyReset 替换为：Channel 1 GlassOrb
///   - Spinner.OnUpgrade 替换为：费用改为 0
/// </summary>
[HarmonyPatch(typeof(Spinner), "OnPlay")]
public static class SpinnerOnPlayPatch
{
    static bool Prefix(Spinner __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext);
        return false;
    }

    static async Task NewOnPlay(Spinner instance, PlayerChoiceContext choiceContext)
    {
        var player = instance.Owner!;
        await OrbCmd.Channel<GlassOrb>(choiceContext, player);
        await PowerCmd.Apply<SpinnerPower>(choiceContext, player.Creature, 1m, player.Creature, null, false);
    }
}

/// <summary>
/// SpinnerPower 在能量重置（回合开始）时注入 1 颗玻璃法球。
/// </summary>
[HarmonyPatch(typeof(SpinnerPower), "AfterEnergyReset")]
public static class SpinnerPowerAfterEnergyResetPatch
{
    static bool Prefix(SpinnerPower __instance, ref Task __result)
    {
        __result = NewAfterEnergyReset(__instance);
        return false;
    }

    static async Task NewAfterEnergyReset(SpinnerPower instance)
    {
        var player = instance.Owner?.Player;
        if (player == null) return;

        await OrbCmd.Channel<GlassOrb>(new ThrowingPlayerChoiceContext(), player);
    }
}

[HarmonyPatch]
public static class SpinnerUpgradePatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        // Spinner 没有自己的 OnUpgrade — 回退到基类
        yield return AccessTools.Method(typeof(Spinner), "OnUpgrade")
                  ?? AccessTools.Method(typeof(CardModel), "OnUpgrade");
    }

    static bool Prefix(CardModel __instance)
    {
        if (__instance is not Spinner) return true;
        var energyCost = __instance.EnergyCost;
        var energyCostType = energyCost.GetType();
        AccessTools.Field(energyCostType, "_base")?.SetValue(energyCost, 0);
        AccessTools.Field(energyCostType, "<Canonical>k__BackingField")?.SetValue(energyCost, 0);
        energyCost.FinalizeUpgrade();
        return false;
    }
}
