using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Common;

public class TokyoTower() : CuteSakikoModEggCard(3, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(35m, ValueProp.Move)
    ];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 1. 提前记录目标位置和容器
        var targetNode = cardPlay.Target.GetCreatureNode();
        var vfxPosition = targetNode?.VfxSpawnPosition ?? targetNode?.GlobalPosition ?? Vector2.Zero;
        var targetContainer = cardPlay.Target.GetVfxContainer() ?? NCombatRoom.Instance?.CombatVfxContainer;

        // 2. 播放飞塔动画
        var vfxScene = GD.Load<PackedScene>("res://CuteSakikoMod/scenes/vfx/tokyo_tower.tscn");
        var vfxInstance = vfxScene?.Instantiate<Node2D>();
        if (vfxInstance != null && targetContainer != null)
        {
            targetContainer.AddChild(vfxInstance);
            vfxInstance.GlobalPosition = vfxPosition;

            var anim = vfxInstance.GetNode<AnimationPlayer>("Sprite2D/AnimationPlayer");
            anim.Play("attack");
            anim.AnimationFinished += name => vfxInstance.QueueFree();
        }

        // 3. 等待动画飞入 (0.8 秒)
        await Cmd.CustomScaledWait(0.25f, 0.8f);

        // 4. 造成伤害并记录结果
        var damageResults = (await CreatureCmd.Damage(
            choiceContext,
            cardPlay.Target,
            DynamicVars.Damage.BaseValue,
            ValueProp.Move,
            this)).ToList();

        // 5. 对目标的其他队友造成同等伤害（参考 Omnislice 的实现）
        var primaryResult = damageResults.FirstOrDefault();
        if (primaryResult != null)
        {
            var totalDamage = primaryResult.TotalDamage + primaryResult.OverkillDamage;
            var otherEnemies = Owner.Creature.CombatState
                .GetTeammatesOf(primaryResult.Receiver)
                .Except(new[] { cardPlay.Target })
                .Where(e => e.IsHittable)
                .ToList();

            if (otherEnemies.Count > 0)
                await CreatureCmd.Damage(
                    choiceContext,
                    otherEnemies,
                    totalDamage,
                    ValueProp.Unpowered | ValueProp.Move,
                    Owner.Creature,
                    this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(15m);
    }
}