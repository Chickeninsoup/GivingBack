using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 重做 Feral：
///   2 费（升级后 1 费）。
///   获得 2 点专注（FocusPower，持续至战斗结束，同 Defragment）。
///   失去 1 个法球槽位。
/// </summary>
[HarmonyPatch(typeof(Feral), "OnPlay")]
public static class FeralOnPlayPatch
{
    static bool Prefix(Feral __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext);
        return false;
    }

    static async Task NewOnPlay(Feral instance, PlayerChoiceContext choiceContext)
    {
        var player = instance.Owner!;

        // 获得 2 点专注（同 Defragment，持续至战斗结束）
        await PowerCmd.Apply<FocusPower>(choiceContext, player.Creature, 2m, player.Creature, null, false);

        // 失去 1 个法球槽位（使用原版 OrbCmd.RemoveSlots，同 BulkUp 等实现方式）
        OrbCmd.RemoveSlots(player, 1);
    }
}

[HarmonyPatch(typeof(Feral), "OnUpgrade")]
public static class FeralUpgradePatch
{
    static bool Prefix(Feral __instance)
    {
        __instance.EnergyCost.UpgradeBy(-1);
        __instance.EnergyCost.FinalizeUpgrade();
        return false;
    }
}
