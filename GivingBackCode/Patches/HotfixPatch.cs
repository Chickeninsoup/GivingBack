using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// Hotfix 修改：
///   移除原版 Exhaust 关键字。
///   升级后效果替换为：本回合获得 3 点集中（TemporaryFocusPower）。
/// </summary>
[HarmonyPatch(typeof(Hotfix), "get_CanonicalKeywords")]
public static class HotfixCanonicalKeywordsPatch
{
    [HarmonyPostfix]
    static void RemoveExhaust(ref IEnumerable<CardKeyword> __result)
    {
        __result = __result.Where(k => k != CardKeyword.Exhaust);
    }
}

[HarmonyPatch(typeof(Hotfix), "OnPlay")]
public static class HotfixUpgradedOnPlayPatch
{
    static bool Prefix(Hotfix __instance, PlayerChoiceContext choiceContext, ref Task __result)
    {
        if (!__instance.IsUpgraded) return true;

        __result = GainTempFocus(__instance, choiceContext);
        return false;
    }

    static async Task GainTempFocus(Hotfix instance, PlayerChoiceContext choiceContext)
    {
        var player = instance.Owner!;
        await PowerCmd.Apply<TemporaryFocusPower>(choiceContext, player.Creature, 3m, player.Creature, null, false);
    }
}
