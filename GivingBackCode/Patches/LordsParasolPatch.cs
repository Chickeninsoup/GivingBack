using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 修改 Lord's Parasol 遗物：
/// 在原有"进商店免费买走所有物品"效果之后，
/// 额外删除牌库中所有 Strike 和 Defend。
/// </summary>
[HarmonyPatch(typeof(LordsParasol), "PurchaseEverything")]
public static class LordsParasolRemoveStartersPatch
{
    // 用 Postfix 包裹原方法返回的 Task：等原逻辑执行完后再删牌
    [HarmonyPostfix]
    static void WrapWithRemove(MerchantInventory inventory, ref Task __result)
    {
        var originalTask = __result;
        __result = RemoveStartersAfterPurchase(originalTask, inventory.Player);
    }

    static async Task RemoveStartersAfterPurchase(Task original, Player player)
    {
        await original;

        var deckPile = CardPile.Get(PileType.Deck, player);
        if (deckPile == null) return;

        var toRemove = deckPile.Cards
            .Where(c => c.IsBasicStrikeOrDefend)
            .ToList();

        if (toRemove.Count > 0)
            await CardPileCmd.RemoveFromDeck(toRemove, true);
    }
}
