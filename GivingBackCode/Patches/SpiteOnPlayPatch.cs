using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using BaseLib.Utils;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 将 Spite 的 OnPlay 替换为：造成 Damage 伤害，若本回合受到 HP 伤害则摸 1 张牌。
/// 原版 beta 行为：Deal 5 damage. If you lost HP this turn, hits 2 times.
/// 目标行为：     Deal 6(9) damage. If you lost HP this turn, draw 1 card.
/// </summary>
[HarmonyPatch(typeof(Spite), "OnPlay")]
public static class SpiteOnPlayPatch
{
    static bool Prefix(Spite __instance, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, cardPlay);
        return false;
    }

    static async Task NewOnPlay(Spite instance, CardPlay cardPlay)
    {
        // 造成 Damage 点伤害（base 6，升级后 9）
        await CommonActions.CardAttack(instance, cardPlay).Execute(null);

        // 若本回合受到过 HP 伤害，摸 1 张牌
        // LostHpThisTurn(Creature) 是 Spite 上的 public 方法
        if (Spite.LostHpThisTurn(instance.Owner.Creature))
        {
            await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 1m, instance.Owner);
        }
    }
}
