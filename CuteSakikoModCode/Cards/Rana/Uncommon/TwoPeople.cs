using System;
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

        // 1. 将目标生命直接设置为当前值的百分比（不造成伤害，无视格挡/力量等）
        decimal percent = IsUpgraded ? 0.45m : 0.50m;
        decimal newHp = Math.Max(1, (int)(targetCurrentHp * percent)); // 至少保留 1 点生命
        await CreatureCmd.SetCurrentHp(target, newHp);

        // 如果目标意外死亡或没有怪物模板，则停止召唤
        if (target.IsDead || target.Monster == null) return;

        // 2. 召唤一个相同的敌人
        var monsterTemplate = ModelDb.GetById<MonsterModel>(target.Monster.Id).ToMutable();
        decimal summonPercent = IsUpgraded ? 0.35m : 0.40m;
        decimal summonHp = Math.Max(targetCurrentHp * summonPercent, 1);

        // 动态分配合法槽位
        string? slotName = null;
        var encounter = CombatState.Encounter;
        if (encounter != null && encounter.Slots.Count > 0)
        {
            var usedSlots = CombatState.Enemies.Select(e => e.SlotName).ToHashSet();
            var availableSlots = encounter.Slots.Where(s => !usedSlots.Contains(s)).ToList();
            if (availableSlots.Count > 0)
            {
                var rng = Owner.RunState.Rng.Shuffle;
                slotName = availableSlots[rng.NextInt(availableSlots.Count)];
            }
            else
            {
                slotName = encounter.Slots[0];
            }
        }

        try
        {
            var summoned = await CreatureCmd.Add(monsterTemplate, CombatState, CombatSide.Enemy, slotName);
            if (summoned != null && !summoned.IsDead)
            {
                await CreatureCmd.SetMaxHp(summoned, summonHp);
                if (!summoned.IsDead)
                    await CreatureCmd.Heal(summoned, summonHp);

                // 随机偏移位置（避免重叠）
                var creatureNode = NCombatRoom.Instance?.GetCreatureNode(summoned);
                if (creatureNode != null)
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
        }
        catch (InvalidOperationException)
        {
            // 某些怪物中途添加可能因状态机异常而失败，忽略召唤，只降低生命值
        }
    }

    protected override void OnUpgrade()
    {
        // 效果已在 IsUpgraded 中处理
    }
}