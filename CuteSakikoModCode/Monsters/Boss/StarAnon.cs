
using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Curse;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;

[RegisterMonster]
public class StarAnon : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 420, 400);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 470, 450);

    
    //自动转换角色tscn节点
    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        // 注意：MonsterAssetProfile 的属性是 VisualsScenePath，不是 Scenes.VisualsPath
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://CuteSakikoMod/scenes/monster/star_anon_boss.tscn"
    );
    
    public override async Task AfterAddedToRoom()
    {
        await PowerCmd.Apply<TimeWatchPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 意图1：给自己上逆跳 (仅一次)
        var buffMove = new MoveState("BUFF", BuffMove, new BuffIntent());

        // 意图2：攻击10*2，塞飞返牌
        var attack2 = new MoveState("DOUBLE_ATTACK", DoubleAttackMove,
            new MultiAttackIntent(10, 2),new StatusIntent(10));

        // 意图3：重击30
        var heavyAttack = new MoveState("HEAVY_ATTACK", HeavyAttackMove,
            new SingleAttackIntent(30));

        // 意图4：提升力量
        var buffStr = new MoveState("BUFF_STRENGTH", BuffStrengthMove,
            new BuffIntent());

        // 状态转换：初始BUFF -> 攻击2 -> 重击 -> 力量 -> 攻击2 -> ...
        buffMove.FollowUpState = attack2;
        attack2.FollowUpState = heavyAttack;
        heavyAttack.FollowUpState = buffStr;
        buffStr.FollowUpState = attack2;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { buffMove, attack2, heavyAttack, buffStr },
            buffMove);
    }

    private async Task BuffMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<RetrogradePower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    private async Task DoubleAttackMove(IReadOnlyList<Creature> targets)
    {
        // 攻击两次，每次10点
        for (int i = 0; i < 2; i++)
        {
            await DamageCmd.Attack(10)
                .FromMonster(this)
                .Execute(null);
        }
        // 向一个玩家抽牌堆塞入飞返
        var player = targets.FirstOrDefault()?.Player;
        if (player != null)
        {
            await CardPileCmd.AddToCombatAndPreview<Flyback>(targets, PileType.Draw, 10,  null);
        }
    }

    private async Task HeavyAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(30)
            .FromMonster(this)
            .Execute(null);
    }

    
    
    private async Task BuffStrengthMove(IReadOnlyList<Creature> targets)
    {
        int playCount = FlybackManager.Instance.TotalPlayCount;   // 直接从 FlybackManager 获取全局总数
        int reloads = GetReloadCount();
        int amount = 1 + (int)((playCount / 100f) * reloads);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, amount, Creature, null);
    }

    private static int GetReloadCount()
    {
        var field = typeof(RunManager).GetField("_numReloads", BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null ? (int)field.GetValue(RunManager.Instance) : 0;
    }
}