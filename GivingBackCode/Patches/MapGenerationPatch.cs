using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 地图生成修改：
/// 1. 每张地图问号（?）数量 +3（减少等量普通战斗）
/// 2. 问号中出现怪物的概率改为 20%
/// 3. 每张地图最多触发一场问号怪物战斗（之后怪物概率归零）
/// </summary>

// ── 状态追踪（以 RunState 为 key，随 Run 结束自动 GC）────────────────────

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

// ── Part 1：每张地图问号数量 +3 ────────────────────────────────────────────
// ActModel.GetMapPointTypes 是抽象方法，不能直接 patch；
// 改为 patch 四个具体 Act 的重写实现。

[HarmonyPatch]
public static class MapUnknownCountPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Glory),      "GetMapPointTypes");
        yield return AccessTools.Method(typeof(Hive),       "GetMapPointTypes");
        yield return AccessTools.Method(typeof(Overgrowth), "GetMapPointTypes");
        yield return AccessTools.Method(typeof(Underdocks), "GetMapPointTypes");
    }

    [HarmonyPostfix]
    static void AddThreeUnknowns(ref MapPointTypeCounts __result)
    {
        __result = new MapPointTypeCounts(__result.NumOfUnknowns + 4, __result.NumOfRests);
    }
}

// ── 进入新 Act 时重置怪物战斗标记 ─────────────────────────────────────────

[HarmonyPatch(typeof(RunManager), "EnterAct")]
public static class MapCreateResetPatch
{
    [HarmonyPrefix]
    static void ResetMonsterFlag(RunManager __instance)
    {
        var runState = __instance.State;
        MainFile.Logger.Info($"[MapPatch] EnterAct called, runState={(runState == null ? "null" : "ok")}, resetting monster flag");
        if (runState != null)
            UnknownMonsterTracker.ResetForMap(runState);
    }
}

// ── Part 2 & 3：控制怪物概率 / 追踪是否已触发战斗 ─────────────────────────

[HarmonyPatch(typeof(UnknownMapPointOdds), "Roll")]
public static class UnknownMapPointRollPatch
{
    // Prefix：在 Roll 前将怪物概率设置为 20%（或 0%，如本地图已出现过怪物）
    [HarmonyPrefix]
    static void SetMonsterOdds(UnknownMapPointOdds __instance, RunState runState)
    {
        if (runState == null) { MainFile.Logger.Info("[MapPatch] Roll: runState is null, skipping"); return; }
        bool monsterFought = UnknownMonsterTracker.Get(runState).MonsterFoughtThisMap;
        __instance.SetBaseOdds(RoomType.Monster, monsterFought ? 0f : 0.2f);
        MainFile.Logger.Info($"[MapPatch] Roll Prefix: monsterFought={monsterFought}, MonsterOdds set to {(monsterFought ? 0f : 0.2f)}");
    }

    // Postfix：Roll 结果为怪物时标记本地图已触发战斗
    [HarmonyPostfix]
    static void TrackMonsterRoll(RoomType __result, RunState runState)
    {
        if (runState == null) return;
        MainFile.Logger.Info($"[MapPatch] Roll result: {__result}");
        if (__result == RoomType.Monster)
        {
            UnknownMonsterTracker.Get(runState).MonsterFoughtThisMap = true;
            MainFile.Logger.Info("[MapPatch] Monster fight flagged for this map");
        }
    }
}
