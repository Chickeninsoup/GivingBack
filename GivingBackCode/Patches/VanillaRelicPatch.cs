using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 修改原版遗物的稀有度。
/// dll 中发现 SetRelicRarityOverride / GetRelicRarityOverride 方法，
/// 优先尝试调用该方法；若不可访问则用 AccessTools 直接设置字段。
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public static class VanillaRelicPatch
{
    [HarmonyPostfix]
    static void ModifyVanillaRelics()
    {
        // ── 方式一：尝试调用游戏内置的 SetRelicRarityOverride（更安全）──
        // var setOverride = AccessTools.Method(typeof(RelicModel), "SetRelicRarityOverride");
        // var burningBlood = ModelDb.AllRelics.OfType<BurningBlood>().FirstOrDefault();
        // if (burningBlood != null && setOverride != null)
        //     setOverride.Invoke(burningBlood, [RelicRarity.Common]);

        // ── 方式二：直接修改 Rarity backing field ─────────────────
        // var rarityField = AccessTools.Field(typeof(RelicModel), "<Rarity>k__BackingField");
        // var burningBlood = ModelDb.AllRelics.OfType<BurningBlood>().FirstOrDefault();
        // if (burningBlood != null)
        //     rarityField.SetValue(burningBlood, RelicRarity.Common);

        // 营养蚝 (Nutritious Oyster) MaxHp 11 -> 22
        var nutritiousOyster = ModelDb.AllRelics.OfType<NutritiousOyster>().FirstOrDefault();
        if (nutritiousOyster != null)
            nutritiousOyster.DynamicVars["MaxHp"].BaseValue = 22m;

        // ── 在此添加你的遗物修改 ─────────────────────────────────
        // 可用的遗物类（命名空间 MegaCrit.Sts2.Core.Models.Relics）：
        //   BurningBlood, BlackBlood, Anchor, ArtOfWar, Akabeko,
        //   BagOfMarbles, BloodVial, BronzeScales, ... 等
        //
        // RelicRarity 枚举（MegaCrit.Sts2.Core.Entities.Relics.RelicRarity）
    }
}
