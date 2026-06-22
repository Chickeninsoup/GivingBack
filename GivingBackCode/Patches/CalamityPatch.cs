using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 重做 Calamity 为 Not Yet：
///   1 费 Skill，EXHAUST。
///   效果：治疗 10 点生命值（升级后 13 点）。
/// </summary>

/// <summary>
/// 替换 OnPlay：治疗 10（升级后 13）点生命值。
/// </summary>
[HarmonyPatch(typeof(Calamity), "OnPlay")]
public static class CalamityOnPlayPatch
{
    static bool Prefix(Calamity __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance);
        return false;
    }

    static async Task NewOnPlay(Calamity instance)
    {
        var player = instance.Owner!;
        var healAmount = instance.IsUpgraded ? 13m : 10m;
        await CreatureCmd.Heal(player.Creature, healAmount, true);
    }
}

/// <summary>
/// 替换 OnUpgrade：升级时将 Heal DynamicVar 改为 13，费用 FinalizeUpgrade。
/// </summary>
[HarmonyPatch(typeof(Calamity), "OnUpgrade")]
public static class CalamityOnUpgradePatch
{
    static bool Prefix(Calamity __instance)
    {
        __instance.EnergyCost.FinalizeUpgrade();
        return false;
    }
}
