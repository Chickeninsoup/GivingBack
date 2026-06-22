using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 兼容修复：UnlockAllCharacters mod 依赖已移除的 CharacterModel.IsUnlocked 属性，
/// 导致初始化失败。此 patch 在 GivingBack 中实现相同效果：
///   - CharacterModel.IsPlayable 始终返回 true（原 IsUnlocked 的替代）
///   - NCharacterSelectButton.RefreshState 执行后强制清除锁定标志
/// </summary>
[HarmonyPatch(typeof(CharacterModel), "get_IsPlayable")]
public static class CharacterAlwaysPlayablePatch
{
    static void Postfix(ref bool __result)
    {
        __result = true;
    }
}

[HarmonyPatch(typeof(NCharacterSelectButton), "RefreshState")]
public static class CharacterButtonClearLockPatch
{
    private static readonly FieldInfo IsLockedField =
        AccessTools.Field(typeof(NCharacterSelectButton), "_isLocked");

    static void Postfix(NCharacterSelectButton __instance)
    {
        IsLockedField?.SetValue(__instance, false);
    }
}
