using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 重做 Hailstorm：
///   1 费 Skill，对一名敌人造成 6 + 3×(当前法球槽中冰霜球数量) 点伤害。
///   升级后每颗冰霜球额外伤害 +1（3→4）。
///
/// 实现参考：
///   - 冰霜球计数参考 Voltalic：OrbQueue.Orbs.Count(o => o is FrostOrb)
///   - 实时伤害显示参考 AshenStrike：CalculatedDamageVar + WithMultiplier
///     （DynamicVars 初始化在 VanillaCardPatch 中完成）
/// </summary>
[HarmonyPatch(typeof(Hailstorm), "OnPlay")]
public static class HailstormOnPlayPatch
{
    static bool Prefix(Hailstorm __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    static async Task NewOnPlay(Hailstorm instance, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        await DamageCmd.Attack(instance.DynamicVars.CalculatedDamage)
            .FromCard(instance)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }
}

[HarmonyPatch(typeof(Hailstorm), "OnUpgrade")]
public static class HailstormUpgradePatch
{
    static bool Prefix(Hailstorm __instance)
    {
        if (__instance.DynamicVars.ContainsKey("ExtraDamage"))
            __instance.DynamicVars["ExtraDamage"].UpgradeValueBy(1m);
        __instance.EnergyCost.FinalizeUpgrade();
        return false;
    }
}
