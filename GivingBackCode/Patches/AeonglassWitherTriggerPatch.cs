using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 战斗开始时，向抽牌堆底部塞入 floor(卡组大小 / 5) 张 Wither。
/// 以 AbstractModel.BeforeCombatStart 为入口，仅在 WitheringPresencePower 实例上触发。
/// </summary>
[HarmonyPatch(typeof(AbstractModel), "BeforeCombatStart")]
public static class AeonglassCombatStartWitherPatch
{
    static void Postfix(AbstractModel __instance, ref Task __result)
    {
        if (__instance is not WitheringPresencePower power) return;
        var player = power.Target.Player;
        if (player == null) return;

        var deckSize = CardPile.Get(PileType.Deck, player)?.Cards.Count ?? 0;
        var count = deckSize / 5;
        if (count <= 0) return;

        var prev = __result;
        __result = AddWithers(prev, power, count);
    }

    static async Task AddWithers(Task prev, WitheringPresencePower power, int count)
    {
        await prev;
        await CardPileCmd.AddToCombatAndPreview<Wither>(
            power.Target, PileType.Draw, count, (Player?)null, CardPilePosition.Bottom);
        power.Flash();
    }
}

/// <summary>
/// 修改 Aeonglass 的 WitheringPresencePower：
/// 每 4 张牌触发一次，使战斗中所有 Wither 牌伤害 +3。
/// </summary>
[HarmonyPatch(typeof(WitheringPresencePower), "AfterCardPlayed")]
public static class WitheringPresenceAfterCardPlayedPatch
{
    private const int TriggerInterval = 4;

    private static readonly FieldInfo CardsLeftKeyField =
        AccessTools.Field(typeof(WitheringPresencePower), "_cardsLeftKey");

    static bool Prefix(WitheringPresencePower __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewAfterCardPlayed(__instance, cardPlay);
        return false;
    }

    static async Task NewAfterCardPlayed(WitheringPresencePower instance, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != instance.Target.Player) return;

        var key = (string)CardsLeftKeyField.GetValue(instance)!;
        var counter = instance.DynamicVars[key];

        if (counter.BaseValue > TriggerInterval) counter.BaseValue = TriggerInterval;

        counter.BaseValue--;
        instance.InvokeDisplayAmountChanged();

        if (counter.IntValue > 0) return;

        await Cmd.Wait(0f, false);

        // 给战斗中所有 Wither 牌伤害 +3
        var player = instance.Target.Player!;
        foreach (var pileType in new[] { PileType.Draw, PileType.Hand, PileType.Discard })
        {
            var pile = CardPile.Get(pileType, player);
            if (pile == null) continue;
            foreach (var wither in pile.Cards.OfType<Wither>())
            {
                WitherBonusDamageTracker.AddBonus(wither, 3m);
                wither.DynamicVars["Damage"].BaseValue += 3m;
            }
        }

        instance.Flash();
        counter.BaseValue = TriggerInterval;
        instance.InvokeDisplayAmountChanged();
    }
}

/// <summary>
/// Aeonglass 强化回合（IncreasingIntensityMove）完整替换：
///   保留：FakeUpgrade 现有 Wither、WitherUpgradeCount++、StrengthPower、AdditionalStrength++
///   去除：AddToCombatAndPreview（不再往牌库塞新 Wither）
///   新增：将弃牌堆/消耗堆所有 Wither 随机洗回抽牌堆，所有 Wither 伤害 +3
/// </summary>
[HarmonyPatch(typeof(Aeonglass), "IncreasingIntensityMove")]
public static class AeonglassIncreasingIntensityPatch
{
    static bool Prefix(Aeonglass __instance, IReadOnlyList<Creature> targets, ref Task __result)
    {
        __result = NewIncreasingIntensity(__instance, targets);
        return false;
    }

    static async Task NewIncreasingIntensity(Aeonglass instance, IReadOnlyList<Creature> targets)
    {
        // 原版：对战斗中所有现有 Wither 执行 FakeUpgrade，并递增计数
        foreach (var target in targets)
        {
            if (target.Player?.PlayerCombatState == null) continue;
            foreach (var card in target.Player.PlayerCombatState.AllCards)
            {
                if (card is Wither wither) wither.FakeUpgrade();
            }
        }
        instance.WitherUpgradeCount++;

        // 新行为：将弃牌堆 / 消耗堆的 Wither 随机洗回抽牌堆（不塞新 Wither）
        var player = targets.Select(t => t.Player).FirstOrDefault(p => p != null);
        if (player != null)
        {
            foreach (var pileType in new[] { PileType.Discard, PileType.Exhaust })
            {
                var pile = CardPile.Get(pileType, player);
                if (pile == null) continue;
                foreach (var wither in pile.Cards.OfType<Wither>().ToList())
                    await CardPileCmd.Add(wither, PileType.Draw, CardPilePosition.Random, null, false);
            }

            // 所有 Wither 伤害 +3（含刚移回的）
            foreach (var pileType in new[] { PileType.Draw, PileType.Hand, PileType.Discard })
            {
                var pile = CardPile.Get(pileType, player);
                if (pile == null) continue;
                foreach (var wither in pile.Cards.OfType<Wither>())
                {
                    WitherBonusDamageTracker.AddBonus(wither, 3m);
                    wither.DynamicVars["Damage"].BaseValue += 3m;
                }
            }
        }

        // 原版：对自身施加 StrengthPower，递增 AdditionalStrength
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), instance.Creature, instance.IncreasingIntensityTotalStrength, instance.Creature, null);
        instance.AdditionalStrength++;
    }
}
