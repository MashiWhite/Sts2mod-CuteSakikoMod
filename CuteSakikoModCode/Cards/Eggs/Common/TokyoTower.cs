using CuteSakikoMod.CuteSakikoModCode.Others;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Common;

public class TokyoTower() : CuteSakikoModEggCard(3, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(35m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 1. 获取目标位置和容器
        var targetNode = cardPlay.Target.GetCreatureNode();
        var vfxPosition = targetNode?.VfxSpawnPosition ?? targetNode?.GlobalPosition ?? Vector2.Zero;
        var container = cardPlay.Target.GetVfxContainer() ?? NCombatRoom.Instance?.CombatVfxContainer;

        // 2. 使用 VFXUtil 实例化特效（已缓存，不会卡顿）
        var vfx = VFXUtil.GenVFXNode("res://CuteSakikoMod/scenes/vfx/tokyo_tower.tscn");
        if (vfx != null && container != null)
        {
            container.AddChildSafely(vfx);
            vfx.GlobalPosition = vfxPosition;

            var anim = vfx.GetNode<AnimationPlayer>("Sprite2D/AnimationPlayer");
            anim.Play("attack");
            // 动画结束后自动销毁
            anim.AnimationFinished += _ => vfx.QueueFreeSafely();
        }

        // 3. 等待动画飞入
        await Cmd.CustomScaledWait(0.25f, 0.8f);

        // 4. 造成伤害
        var damageResults = (await CreatureCmd.Damage(
            choiceContext, cardPlay.Target, DynamicVars.Damage.BaseValue,
            ValueProp.Move, this)).ToList();

        // 5. 对其他敌方队友造成同等伤害
        var primary = damageResults.FirstOrDefault();
        if (primary != null)
        {
            var totalDamage = primary.TotalDamage + primary.OverkillDamage;
            var otherEnemies = Owner.Creature.CombatState
                .GetTeammatesOf(primary.Receiver)
                .Except(new[] { cardPlay.Target })
                .Where(e => e.IsHittable)
                .ToList();

            if (otherEnemies.Count > 0)
                await CreatureCmd.Damage(choiceContext, otherEnemies,
                    totalDamage, ValueProp.Unpowered | ValueProp.Move,
                    Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(15m);
    }
}