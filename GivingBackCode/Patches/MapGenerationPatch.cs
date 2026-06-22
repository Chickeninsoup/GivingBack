using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 地图问号机制修改：
/// 1. 问号中出现怪物的概率改为 20%
/// 2. 每层地图最多触发一场问号怪物战斗（之后怪物概率归零）
/// </summary>

// ── 状态追踪（以 RunState 为 key，随 Run 结束自动 GC）────────────────────────

internal static class UnknownMonsterTracker
{
    private static readonly ConditionalWeakTable<RunState, State> Table = new();

    public static State Get(RunState runState) => Table.GetOrCreateValue(runState);

    public static void ResetForMap(RunState runState) => Get(runState).MonsterFoughtThisMap = false;

    internal sealed class State
    {
        public bool MonsterFoughtThisMap;
    }
}

// ── 进入新 Act 时重置怪物战斗标记 ────────────────────────────────────────────

[HarmonyPatch(typeof(RunManager), "EnterAct")]
public static class MapCreateResetPatch
{
    [HarmonyPrefix]
    static void ResetMonsterFlag(RunManager __instance)
    {
        var runState = __instance.State;
        if (runState != null)
            UnknownMonsterTracker.ResetForMap(runState);
    }
}

// ── 控制问号怪物概率 / 追踪是否已触发战斗 ───────────────────────────────────

[HarmonyPatch(typeof(UnknownMapPointOdds), "Roll")]
public static class UnknownMapPointRollPatch
{
    // Prefix：将怪物概率设为 20%；若本层已出现过怪物则归零
    [HarmonyPrefix]
    static void SetMonsterOdds(UnknownMapPointOdds __instance, RunState runState)
    {
        if (runState == null) return;
        bool alreadyFought = UnknownMonsterTracker.Get(runState).MonsterFoughtThisMap;
        __instance.SetBaseOdds(RoomType.Monster, alreadyFought ? 0f : 0.2f);
    }

    // Postfix：若结果为怪物，标记本层已触发战斗
    [HarmonyPostfix]
    static void TrackMonsterRoll(RoomType __result, RunState runState)
    {
        if (runState == null) return;
        if (__result == RoomType.Monster)
            UnknownMonsterTracker.Get(runState).MonsterFoughtThisMap = true;
    }
}
