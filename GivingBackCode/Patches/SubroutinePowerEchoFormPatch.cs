using System.Collections;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 使 SubroutinePower 在回响形态（EchoForm）重打能力牌时也能给费。
///
/// 原版 SubroutinePower.AfterCardPlayed 用 amountsForPlayedCards 字段做去重：
/// 首次触发后将该牌记录进字典，下次（回响重打）再见到同一张牌就跳过。
/// Prefix 在回响重打时（IsFirstInSeries == false）提前将该牌从字典中移除，
/// 让原方法以为是首次触发，从而正常给费并重新记录。
/// </summary>
[HarmonyPatch(typeof(SubroutinePower), "AfterCardPlayed")]
public static class SubroutinePowerEchoFormPatch
{
    private static readonly FieldInfo? DataField;
    private static readonly FieldInfo? AmountsField;

    static SubroutinePowerEchoFormPatch()
    {
        // 找到 SubroutinePower 内部的嵌套类 Data
        var dataType = typeof(SubroutinePower)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(t => t.Name == "Data");

        if (dataType == null)
        {
            MainFile.Logger.Error("SubroutinePowerEchoFormPatch: SubroutinePower.Data 嵌套类未找到。");
            return;
        }

        // 找到 SubroutinePower 中持有 Data 实例的字段（_data）
        DataField = typeof(SubroutinePower)
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(f => f.FieldType == dataType);

        if (DataField == null)
        {
            MainFile.Logger.Error("SubroutinePowerEchoFormPatch: SubroutinePower._data 字段未找到。");
            return;
        }

        // 找到 Data 中的 amountsForPlayedCards 字段
        AmountsField = dataType
            .GetField("amountsForPlayedCards",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (AmountsField == null)
        {
            MainFile.Logger.Error("SubroutinePowerEchoFormPatch: amountsForPlayedCards 字段未找到。");
        }
    }

    [HarmonyPrefix]
    static void Prefix(SubroutinePower __instance, CardPlay cardPlay)
    {
        // 仅对回响重打（非首次出牌）生效
        if (cardPlay.IsFirstInSeries) return;
        if (DataField == null || AmountsField == null) return;

        var data = DataField.GetValue(__instance);
        if (data == null) return;

        // 将当前牌从去重字典中移除，使原方法能重新触发给费
        var amounts = AmountsField.GetValue(data) as IDictionary;
        amounts?.Remove(cardPlay.Card);
    }
}
