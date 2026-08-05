
using Godot;
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

        // 1. 降低目标生命（不造成伤害，所以直接设置）
        int targetCurrentHp = target.CurrentHp;
        decimal percent = IsUpgraded ? 0.45m : 0.50m;
        decimal newHp = Math.Max(1, (int)(targetCurrentHp * percent));
        await CreatureCmd.SetCurrentHp(target, newHp);

        // 2. 检查是否可以召唤
        if (target.IsDead || target.Monster == null) return;

        // 3. 准备召唤模板（从 ModelDb 获取原始模型再转为可变副本）
        var monsterTemplate = ModelDb.GetById<MonsterModel>(target.Monster.Id).ToMutable();
        decimal summonPercent = IsUpgraded ? 0.35m : 0.40m;
        decimal summonHp = Math.Max(targetCurrentHp * summonPercent, 1);

        // 4. 分配槽位：优先从遭遇中找一个未被占用的
        string? slotName = null;
        var encounter = CombatState.Encounter;
        if (encounter != null && encounter.Slots.Count > 0)
        {
            var usedSlots = CombatState.Enemies
                .Where(e => e != null && !string.IsNullOrEmpty(e.SlotName))
                .Select(e => e.SlotName)
                .ToHashSet();
        
            // 选第一个空闲槽位（你也可以随机选，但稳定的顺序更可控）
            slotName = encounter.Slots.FirstOrDefault(s => !usedSlots.Contains(s));
            // 如果所有槽位都被占用，slotName 保持 null，之后会触发重新布局
        }

        try
        {
            // 5. 通过 Command 添加怪物（slotName 为 null 时游戏不会报错）
            var summoned = await CreatureCmd.Add(monsterTemplate, CombatState, CombatSide.Enemy, slotName);

            if (summoned != null && !summoned.IsDead)
            {
                // 6. 设置召唤物的最大生命并回满
                await CreatureCmd.SetMaxHp(summoned, summonHp);
                if (!summoned.IsDead)
                    await CreatureCmd.Heal(summoned, summonHp);

                // 7. 如果没有分配到槽位，手动重新排列所有无槽位敌人
                if (slotName == null)
                {
                    RepositionSlotlessEnemies(CombatState);
                }
            }
        }
        catch (InvalidOperationException)
        {
            // 某些怪物可能无法中途添加，忽略即可
        }
    }
    
    private static void RepositionSlotlessEnemies(ICombatState combatState)
    {
        var room = NCombatRoom.Instance;
        if (room == null) return;

        // 收集所有需要布局的敌人节点（非玩家、非宠物、存活）
        var nodes = room.CreatureNodes
            .Where(n => GodotObject.IsInstanceValid(n) && n.Entity != null
                                                       && !n.Entity.IsPlayer && n.Entity.PetOwner == null
                                                       && !n.Entity.IsDead)
            .Take(64)
            .ToArray();

        if (nodes.Length == 0) return;

        // 计算屏幕可用宽度（考虑镜头缩放）
        float scale = combatState.Encounter?.GetCameraScaling() ?? 1f;
        if (!float.IsFinite(scale) || scale <= 0) scale = 1f;
        float totalWidth = 960f / scale;

        // 获取每个怪物的实际视觉宽度
        var widths = nodes.Select(node =>
        {
            float w = node.Visuals?.Bounds.Size.X ?? 0;
            return w > 0 ? w : 120f; // 默认宽度
        }).ToArray();

        float spacing = 70f;
        float sumWidths = widths.Sum();
        float totalUsed = sumWidths + (nodes.Length - 1) * spacing;
        float startX = Math.Max((totalWidth - totalUsed) * 0.5f, 150f);

        float yOffset = 0f;
        if (startX + totalUsed > totalWidth && nodes.Length > 1)
        {
            spacing = Math.Max((totalWidth - 150f - sumWidths) / (nodes.Length - 1), 5f);
            float newTotal = sumWidths + (nodes.Length - 1) * spacing;
            startX = (totalWidth - newTotal) * 0.5f;
            if (spacing < 30f)
                yOffset = Mathf.Lerp(60f, 40f, (spacing - 5f) / 25f);
        }

        float posX = startX;
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].Position = new Vector2(
                posX + widths[i] * 0.5f,
                200f - (i % 2 == 0 ? 0f : yOffset)
            );
            posX += widths[i] + spacing;
        }
    }

    private static bool IsValid(Godot.GodotObject obj) 
        => GodotObject.IsInstanceValid(obj);

    protected override void OnUpgrade()
    {
        // 效果已在 IsUpgraded 中处理
    }
}