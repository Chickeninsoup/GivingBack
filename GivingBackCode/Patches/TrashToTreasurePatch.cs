using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 重做 Trash to Treasure：
///   3 费 Skill，Exhaust。
///   将手牌、牌库、弃牌堆中所有状态牌变形为 Fuel（升级后变形为 Fuel+）。
///
/// 实现参考：
///   - 多牌堆状态牌检测参考 FlakCannon（遍历 Hand/Draw/Discard）
///   - 变形为 Fuel 使用 CardCmd.TransformTo&lt;Fuel&gt;，参考 Compact
///   - Exhaust 由引擎通过 CardKeyword.Exhaust → GetResultPileType 自动处理
/// </summary>
[HarmonyPatch(typeof(TrashToTreasure), "OnPlay")]
public static class TrashToTreasureOnPlayPatch
{
    static bool Prefix(TrashToTreasure __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = NewOnPlay(__instance, choiceContext);
        return false;
    }

    static async Task NewOnPlay(TrashToTreasure instance, PlayerChoiceContext choiceContext)
    {
        var player = instance.Owner!;
        bool upgraded = instance.IsUpgraded;

        // 参考 FlakCannon：遍历手牌、牌库、弃牌堆收集所有状态牌（拍摄快照避免遍历中修改集合）
        var statusCards = new List<CardModel>();
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
        {
            var pile = CardPile.Get(pileType, player);
            if (pile == null) continue;
            foreach (var card in pile.Cards)
            {
                if (card.Type == CardType.Status && card != instance)
                    statusCards.Add(card);
            }
        }

        // 参考 Compact：逐一通过 CardCmd.TransformTo<Fuel> 变形（避免反射，直接调用 API）
        if (upgraded)
        {
            // 升级版：先记录当前所有 Fuel 牌，变形后将新增的 Fuel 升级为 Fuel+
            var existingFuels = new HashSet<CardModel>();
            foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
            {
                var pile = CardPile.Get(pileType, player);
                if (pile == null) continue;
                foreach (var f in pile.Cards.OfType<Fuel>())
                    existingFuels.Add(f);
            }

            foreach (var card in statusCards)
                await CardCmd.TransformTo<Fuel>(card, default(CardPreviewStyle));

            foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
            {
                var pile = CardPile.Get(pileType, player);
                if (pile == null) continue;
                foreach (var fuel in pile.Cards.OfType<Fuel>().Where(f => !existingFuels.Contains(f)).ToList())
                    CardCmd.Upgrade(fuel, default(CardPreviewStyle));
            }
        }
        else
        {
            foreach (var card in statusCards)
                await CardCmd.TransformTo<Fuel>(card, default(CardPreviewStyle));
        }

    }
}

[HarmonyPatch(typeof(TrashToTreasure), "OnUpgrade")]
public static class TrashToTreasureUpgradePatch
{
    static bool Prefix(TrashToTreasure __instance)
    {
        __instance.EnergyCost.FinalizeUpgrade();
        var kw = AccessTools.Field(typeof(CardModel), "_keywords")?.GetValue(__instance) as HashSet<CardKeyword>;
        kw?.Add(CardKeyword.Retain);
        return false;
    }
}
