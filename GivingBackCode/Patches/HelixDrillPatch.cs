using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 重做 Helix Drill：
///   对手牌、抽牌堆、弃牌堆中每张状态牌造成 3 点伤害。
///   升级后每张状态牌伤害 +2（3→5）。
///   Uncommon，移除 Retain。
///
/// 状态牌检测参考 FlakCannon：
///   owner.PlayerCombatState.AllCards.Where(c => c.Type == CardType.Status
///       && c.Pile.Type != PileType.Exhaust)
/// </summary>
[HarmonyPatch(typeof(HelixDrill), "OnPlay")]
public static class HelixDrillOnPlayPatch
{
    static bool Prefix(HelixDrill __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    static async Task NewOnPlay(HelixDrill instance, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var statusCount = instance.Owner!.PlayerCombatState
            .AllCards
            .Count(c => c.Type == CardType.Status && c.Pile.Type != PileType.Exhaust);

        if (statusCount <= 0) return;

        var damagePerHit = instance.DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damagePerHit)
            .WithHitCount(statusCount)
            .FromCard(instance)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}

[HarmonyPatch(typeof(HelixDrill), "OnUpgrade")]
public static class HelixDrillUpgradePatch
{
    static bool Prefix(HelixDrill __instance)
    {
        __instance.DynamicVars.Damage.BaseValue = 5m;
        __instance.EnergyCost.FinalizeUpgrade();
        return false;
    }
}
