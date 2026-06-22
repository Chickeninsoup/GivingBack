using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 增强 Blood Wall：失去 HP 从 2 降为 1。
/// 实现：在原版 OnPlay（失去 2 HP + 获得格挡）执行完毕后，
/// 额外回复 1 HP，净效果 = 失去 1 HP。
/// </summary>
[HarmonyPatch(typeof(BloodWall), "OnPlay")]
public static class BloodWallHpLossPatch
{
    static void Postfix(BloodWall __instance, ref Task __result)
    {
        __result = HealAfterPlay(__instance, __result);
    }

    static async Task HealAfterPlay(BloodWall instance, Task original)
    {
        await original;
        await CreatureCmd.Heal(instance.Owner.Creature, 1m, false);
    }
}
