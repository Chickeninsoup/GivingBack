using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace GivingBack.GivingBackCode.Patches;

/// <summary>
/// 修改 InfestedPrism 的行动循环：
///   原版 4 轮循环：JAB → RADIATE → WHIRLWIND（打 3 次）→ PULSATE → 循环
///   修改后 3 轮循环：JAB → RADIATE → PULSATE → 循环（删除 WHIRLWIND）
/// </summary>
[HarmonyPatch(typeof(InfestedPrism), "GenerateMoveStateMachine")]
public static class InfestedPrismMovePatch
{
    [HarmonyPostfix]
    static void RemoveWhirlwind(ref MonsterMoveStateMachine __result)
    {
        var states = __result.States;

        if (!states.TryGetValue("RADIATE_MOVE", out var radiateState)) return;
        if (!states.TryGetValue("PULSATE_MOVE", out var pulsateState)) return;

        if (radiateState is not MoveState radiateMove) return;

        // 将 RADIATE 的后继从 WHIRLWIND_MOVE 改为 PULSATE_MOVE
        // FollowUpStateId 是 init-only，用反射绕过编译期限制
        AccessTools.Property(typeof(MoveState), "FollowUpStateId")
            ?.SetValue(radiateMove, "PULSATE_MOVE");
        radiateMove.FollowUpState = pulsateState;
    }
}

/// <summary>
/// 修改 PulsateMove：不再给玩家施加 VitalSpark，改为自身获得 6 点力量。
/// </summary>
[HarmonyPatch(typeof(InfestedPrism), "PulsateMove")]
public static class InfestedPrismPulsatePatch
{
    static bool Prefix(InfestedPrism __instance, IReadOnlyList<Creature> targets, ref Task __result)
    {
        __result = NewPulsateMove(__instance, targets);
        return false;
    }

    static async Task NewPulsateMove(InfestedPrism instance, IReadOnlyList<Creature> targets)
    {
        const string sfx = "event:/sfx/enemy/enemy_attacks/infested_prisms/infested_prisms_attack_defend";

        // 攻击（与原版相同）
        await DamageCmd.Attack((decimal)instance.PulsateDamage)
            .FromMonster(instance)
            .WithAttackerAnim("AttackBlock", 0f, null!)
            .WithAttackerFx(null!, sfx, null!)
            .WithHitFx("vfx/vfx_attack_slash", null!, null!)
            .Execute(null!);

        // 格挡（与原版相同）
        await CreatureCmd.GainBlock(instance.Creature, (decimal)instance.PulsateBlock, ValueProp.Move, null!, false);

        // 新行为：自身获得 6 点力量（替换原版 VitalSpark）
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            instance.Creature,
            6m,
            instance.Creature,
            null,
            false);
    }
}
