using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 修改 Synchronize：
///   每有一个充能球（PlasmaOrb），获得 1 点能量。
///   移除所有法球。
///   升级添加 Retain（由 SynchronizeUpgradePatch 处理）。
/// </summary>
[HarmonyPatch(typeof(Synchronize), "OnPlay")]
public static class SynchronizeOnPlayPatch
{
    static bool Prefix(Synchronize __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext);
        return false;
    }

    static async Task NewOnPlay(Synchronize instance, PlayerChoiceContext choiceContext)
    {
        var player = instance.Owner!;

        var orbQueue = player.PlayerCombatState?.OrbQueue;
        int orbCount = orbQueue?.Orbs.Count ?? 0;

        if (orbCount > 0)
            await PlayerCmd.GainEnergy((decimal)orbCount, player);

    }
}
