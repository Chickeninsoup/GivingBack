using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 丝滑发丝（Silken Tress）：限制 TryModifyCardRewardOptionsLate 仅对首个卡牌奖励生效。
/// 不加限制时，vanilla 会对每次卡牌奖励都施加 Glam 附魔（并可能开放重掷）。
/// </summary>
[HarmonyPatch(typeof(SilkenTress), "TryModifyCardRewardOptionsLate")]
public static class SilkenTressFirstRewardOnlyPatch
{
    private static readonly ConditionalWeakTable<SilkenTress, object> _applied = new();
    private static readonly object _sentinel = new();

    static bool Prefix(SilkenTress __instance)
    {
        if (_applied.TryGetValue(__instance, out _))
            return false; // 首个卡牌奖励已处理，跳过后续
        _applied.Add(__instance, _sentinel);
        return true; // 首次：让 vanilla 逻辑正常运行
    }
}

/// <summary>
/// 丝滑发丝（Silken Tress）额外效果：首个卡牌奖励可以刷新一次。
/// 重掷后新生成的牌不会经过 TryModifyCardRewardOptionsLate，无法自动获得附魔，
/// 因此需要在 AfterGenerated 事件中手动对新牌补充 Glam 附魔。
/// </summary>
[HarmonyPatch(typeof(AbstractModel), "TryModifyRewardsLate")]
public static class SilkenTressRerollPatch
{
    private static readonly ConditionalWeakTable<SilkenTress, object> _grantedReroll = new();
    private static readonly object _sentinel = new();

    private static readonly FieldInfo HasBeenRerolledField =
        AccessTools.Field(typeof(CardReward), "_hasBeenRerolled");

    static void Postfix(AbstractModel __instance, Player player, List<Reward> rewards, AbstractRoom room,
        ref bool __result)
    {
        if (__instance is not SilkenTress tress) return;

        var cardRewards = rewards.OfType<CardReward>().ToList();

        if (_grantedReroll.TryGetValue(tress, out _))
        {
            // 首个奖励已处理：强制关闭后续所有卡牌奖励的重掷
            foreach (var cr in cardRewards)
                cr.CanReroll = false;
            return;
        }

        var firstCardReward = cardRewards.FirstOrDefault();
        if (firstCardReward == null) return;

        firstCardReward.CanReroll = true;

        // 订阅 AfterGenerated：reroll 后对新牌重新施加 Glam 附魔
        firstCardReward.AfterGenerated += () => ReapplyGlamEnchantment(firstCardReward);

        _grantedReroll.Add(tress, _sentinel);
        __result = true;
    }

    private static void ReapplyGlamEnchantment(CardReward reward)
    {
        var hasBeenRerolled = (bool)HasBeenRerolledField.GetValue(reward)!;
        if (!hasBeenRerolled) return;

        foreach (var card in reward.Cards)
            CardCmd.Enchant<Glam>(card, 1m);
    }
}
