using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;

namespace GivingBack.GivingBackCode.Patches;

// 移牌费用：起始 75(A6+ 100) + 25(A6+ 50) 每次
// 修改为：起始 50(A6+ 75) + 25(A6+ 25) 每次
[HarmonyPatch(typeof(MerchantCardRemovalEntry), "BaseCost", MethodType.Getter)]
public static class CardRemovalBaseCostPatch
{
    [HarmonyPostfix]
    static void ReduceBaseCost(ref int __result)
    {
        __result -= 25;
    }
}

[HarmonyPatch(typeof(MerchantCardRemovalEntry), "PriceIncrease", MethodType.Getter)]
public static class CardRemovalPriceIncreasePatch
{
    [HarmonyPostfix]
    static void CapPriceIncrease(ref int __result)
    {
        // 正常: 25 -> 25（不变）；Inflation A6+: 50 -> 25
        if (__result > 25)
            __result = 25;
    }
}
