using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Elite;

[RegisterMonster]
public class Jennifer : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 120, 110);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 130, 120);

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/jennifer.tscn"
    );

    // 伤害与格挡数值（高进阶变化）
    private int WhatDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 11);
    private int UnknownDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    private int HmmBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 25);
    private int WhatThe => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 2);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }
    
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<JenniferPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 四个动作，全部使用隐藏意图
        var whatMove = new MoveState("WHAT", WhatMove,
            new UnknownIntent());
        var unknownMove = new MoveState("UNKNOWN", UnknownMove,
            new UnknownIntent());
        var hmmMove = new MoveState("HMM", HmmMove,
            new UnknownIntent());
        var whatTheMove = new MoveState("WHAT_THE", WhatTheMove,
            new UnknownIntent());

        // 随机意图分支：
        var randomState = new RandomBranchState("JENNIFER_RANDOM");
        randomState.AddBranch(whatMove, MoveRepeatType.CanRepeatForever, 0.40f);
        randomState.AddBranch(unknownMove, MoveRepeatType.CanRepeatForever, 0.30f);
        randomState.AddBranch(hmmMove, MoveRepeatType.CanRepeatForever, 0.10f);
        randomState.AddBranch(whatTheMove, MoveRepeatType.CanRepeatForever, 0.20f);

        // 所有动作执行完后回到随机分支，形成循环
        whatMove.FollowUpState = randomState;
        unknownMove.FollowUpState = randomState;
        hmmMove.FollowUpState = randomState;
        whatTheMove.FollowUpState = randomState;

        var states = new List<MonsterState> { randomState, whatMove, unknownMove, hmmMove, whatTheMove };
        return new MonsterMoveStateMachine(states, randomState);
    }

    private async Task WhatMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(WhatDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        PlayRandomSound();
    }

    private async Task UnknownMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(UnknownDamage)
            .FromMonster(this)
            .WithHitCount(4)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        PlayRandomSound();
    }

    private async Task HmmMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, HmmBlock, ValueProp.Move, null);
        PlayRandomSound();
    }

    private async Task WhatTheMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), Creature, WhatThe, Creature, null);
        PlayRandomSound();
    }

    private static void PlayRandomSound()
    {
        // 1. 随机文件名
        int index = System.Random.Shared.Next(2);
        string file = index == 0 ? "jennifer1.mp3" : "jennifer2.mp3";

        // 2. 拼接绝对路径（与 AnonGuitar 完全一致）
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(dir))
        {
            GD.PrintErr("[Jennifer] 无法获取程序集目录，音频播放失败");
            return;
        }
        var fullPath = Path.Combine(dir, "audio", file);
        GD.Print($"[Jennifer] 即将播放音效: {fullPath}");

        // 3. 调用 AudioManager（内部已处理主线程安全、音量计算等）
        AudioManager.PlaySound(fullPath,1.2f);
    }
}