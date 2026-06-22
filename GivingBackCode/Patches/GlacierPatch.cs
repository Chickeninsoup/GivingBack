using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.ValueProps;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 重做 Glacier：
///   Block 9，注入 1 颗冰霜法球（升级后 2 颗）。
///
/// 完整替换 OnPlay，避免原版通过不存在的 DynamicVar 控制法球数量导致崩溃。
/// OnUpgrade 同样替换，仅 FinalizeUpgrade，不修改 DynamicVars。
/// </summary>
[HarmonyPatch(typeof(Glacier), "OnPlay")]
public static class GlacierOnPlayPatch
{
    static bool Prefix(Glacier __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    static async Task NewOnPlay(Glacier instance, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = instance.Owner!;
        await CreatureCmd.GainBlock(player.Creature, instance.DynamicVars["Block"].BaseValue, ValueProp.Move, cardPlay, false);
        int orbCount = instance.IsUpgraded ? 2 : 1;
        for (int i = 0; i < orbCount; i++)
            await OrbCmd.Channel<FrostOrb>(choiceContext, player);
    }
}

[HarmonyPatch(typeof(Glacier), "OnUpgrade")]
public static class GlacierUpgradePatch
{
    static bool Prefix(Glacier __instance)
    {
        __instance.EnergyCost.FinalizeUpgrade();
        // 升级后法球数量 2，通过 FinalizeUpgrade 使 diff() 正确显示
        if (__instance.DynamicVars.ContainsKey("Repeat"))
            __instance.DynamicVars["Repeat"].FinalizeUpgrade();
        return false;
    }
}
