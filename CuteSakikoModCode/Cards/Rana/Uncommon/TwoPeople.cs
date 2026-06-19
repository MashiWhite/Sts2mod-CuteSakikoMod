using System.Collections.Generic;
using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class TwoPeople() : CuteRanaCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private static readonly Dictionary<ICombatState, List<Godot.Vector2>> OccupiedPositions = new();

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        var target = cardPlay.Target;

        if (target.IsDead) return;

        int targetCurrentHp = target.CurrentHp;

        // 1. 将目标生命降低
        decimal damagePercent = IsUpgraded ? 0.55m : 0.50m;
        decimal loseHp = targetCurrentHp * damagePercent;
        await CreatureCmd.Damage(choiceContext, target, loseHp, ValueProp.Unblockable | ValueProp.Move, Owner.Creature, this);

        // 目标可能在伤害后死亡
        if (target.IsDead || target.Monster == null) return;

        // 2. 召唤一个相同的敌人
        var monsterTemplate = ModelDb.GetById<MonsterModel>(target.Monster.Id).ToMutable();
        decimal summonPercent = IsUpgraded ? 0.35m : 0.40m;
        decimal summonHp = Math.Max(targetCurrentHp * summonPercent, 1); // 至少1点血

        var summoned = await CreatureCmd.Add(monsterTemplate, CombatState, CombatSide.Enemy, null);
        if (summoned == null || summoned.IsDead) return;

        // 用 SetMaxHp + Heal 替代 SetMaxAndCurrentHp
        await CreatureCmd.SetMaxHp(summoned, summonHp);
        if (!summoned.IsDead)
            await CreatureCmd.Heal(summoned, summonHp);

        // 不重叠的随机位置
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(summoned);
        if (creatureNode != null && !summoned.IsDead)
        {
            if (!OccupiedPositions.ContainsKey(CombatState))
                OccupiedPositions[CombatState] = new List<Godot.Vector2>();

            var occupied = OccupiedPositions[CombatState];
            var rng = Owner.RunState.Rng.Shuffle;
            const float minDistance = 120f;
            Godot.Vector2 offset;
            int attempts = 0;

            do
            {
                float xOffset = (float)(rng.NextDouble() * 500);
                float yOffset = (float)(rng.NextDouble() * 400 - 200);
                offset = new Godot.Vector2(xOffset, yOffset);
                attempts++;
            }
            while (attempts < 50 && occupied.Any(p => p.DistanceTo(offset) < minDistance));

            creatureNode.Position += offset;
            occupied.Add(offset);
        }
    }

    protected override void OnUpgrade()
    {
        // 效果已在 IsUpgraded 中处理
    }
}