using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Chord;

public static class ChordManager
{
    private static readonly List<string> _temporaryChordIds = new();

    static ChordManager()
    {
        RegisterChords();
    }

    public static Dictionary<string, ChordDefinition> AllChords { get; } = new();
    public static List<ChordDefinition> AllChordsList { get; } = new();

    private static void AddChord(string id, ChordCategory cat, CardType[] seq,
        string titleKey, string descKey, string iconName,
        int[] baseValues,
        Func<PlayerChoiceContext, Creature, int, Task> effect)
    {
        var def = new ChordDefinition
        {
            Id = id,
            Category = cat,
            NoteSequence = seq,
            TitleKey = titleKey,
            DescKey = descKey,
            IconName = iconName,
            BaseValues = baseValues,
            Effect = effect
        };
        AllChords[id] = def;
        AllChordsList.Add(def);
    }

    private static void AddTemporaryChord(string id, ChordCategory cat, CardType[] seq,
        string titleKey, string descKey, string iconName, int[] baseValues,
        Func<PlayerChoiceContext, Creature, int, Task> effect)
    {
        var def = new ChordDefinition
        {
            Id = id,
            Category = cat,
            NoteSequence = seq,
            TitleKey = titleKey,
            DescKey = descKey,
            IconName = iconName,
            BaseValues = baseValues,
            Effect = effect,
            IsTemporaryOnly = true
        };
        AllChords[id] = def;
        AllChordsList.Add(def);
        _temporaryChordIds.Add(id);
    }

    // ChordManager.cs 的 RegisterChords 方法（完整替换）
private static void RegisterChords()
{
    // ==================== 大三和弦 ====================
    // C【攻攻技】对随机敌人造成6点伤害
    AddChord("C", ChordCategory.Major,
        new[] { CardType.Attack, CardType.Attack, CardType.Skill },
        "CUTE_SAKIKO_MOD_CCHORD.title", "CUTE_SAKIKO_MOD_CCHORD.description", "c_chord",
        new[] { 6 },
        async (ctx, owner, bonus) =>
        {
            var combat = owner.CombatState; if (combat == null) return;
            var enemies = combat.HittableEnemies.OrderBy(e => e.Monster?.Id.Entry).ToList();
            if (enemies.Any())
            {
                var target = combat.RunState.Rng.CombatCardSelection.NextItem(enemies);
                await CreatureCmd.Damage(ctx, target, new DamageVar(6 + bonus, ValueProp.Move), owner, null, null);
            }
        });

    // D【攻技攻】对所有敌人造成4点伤害
    AddChord("D", ChordCategory.Major,
        new[] { CardType.Attack, CardType.Skill, CardType.Attack },
        "CUTE_SAKIKO_MOD_DCHORD.title", "CUTE_SAKIKO_MOD_DCHORD.description", "d_chord",
        new[] { 4 },
        async (ctx, owner, bonus) =>
        {
            var enemies = owner.CombatState?.Enemies;
            if (enemies != null)
                await CreatureCmd.Damage(ctx, enemies, new DamageVar(4 + bonus, ValueProp.Move), owner, null, null);
        });

    // E【技攻攻】所有友方本回合获得2点临时力量
    AddChord("E", ChordCategory.Major,
        new[] { CardType.Skill, CardType.Attack, CardType.Attack },
        "CUTE_SAKIKO_MOD_ECHORD.title", "CUTE_SAKIKO_MOD_ECHORD.description", "e_chord",
        new[] { 2 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<ChordTempStrengthPower>(ctx, ally, 2 + bonus, owner, null);
        });

    // F【攻攻攻】给予所有敌人1层易伤
    AddChord("F", ChordCategory.Major,
        new[] { CardType.Attack, CardType.Attack, CardType.Attack },
        "CUTE_SAKIKO_MOD_FCHORD.title", "CUTE_SAKIKO_MOD_FCHORD.description", "f_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            var enemies = owner.CombatState?.Enemies;
            if (enemies != null)
                foreach (var enemy in enemies)
                    await PowerCmd.Apply<VulnerablePower>(ctx, enemy, 1 + bonus, owner, null);
        });

    // G【攻攻攻攻】对所有敌人造成9点伤害
    AddChord("G", ChordCategory.Major,
        new[] { CardType.Attack, CardType.Attack, CardType.Attack, CardType.Attack },
        "CUTE_SAKIKO_MOD_GCHORD.title", "CUTE_SAKIKO_MOD_GCHORD.description", "g_chord",
        new[] { 9 },
        async (ctx, owner, bonus) =>
        {
            var enemies = owner.CombatState?.Enemies;
            if (enemies != null)
                await CreatureCmd.Damage(ctx, enemies, new DamageVar(9 + bonus, ValueProp.Move), owner, null, null);
        });

    // A【攻攻攻技】所有友方获得5层活力
    AddChord("A", ChordCategory.Major,
        new[] { CardType.Attack, CardType.Attack, CardType.Attack, CardType.Skill },
        "CUTE_SAKIKO_MOD_ACHORD.title", "CUTE_SAKIKO_MOD_ACHORD.description", "a_chord",
        new[] { 5 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<VigorPower>(ctx, ally, 5 + bonus, owner, null);
        });

    // B【攻攻技攻】对随机敌人造成3点伤害5次
    AddChord("B", ChordCategory.Major,
        new[] { CardType.Attack, CardType.Attack, CardType.Skill, CardType.Attack },
        "CUTE_SAKIKO_MOD_BCHORD.title", "CUTE_SAKIKO_MOD_BCHORD.description", "b_chord",
        new[] { 3, 5 },
        async (ctx, owner, bonus) =>
        {
            var combat = owner.CombatState; if (combat == null) return;
            var enemies = combat.HittableEnemies.OrderBy(e => e.Monster?.Id.Entry).ToList();
            if (!enemies.Any()) return;
            for (int i = 0; i < 5; i++)
            {
                var target = combat.RunState.Rng.CombatCardSelection.NextItem(enemies);
                await CreatureCmd.Damage(ctx, target, new DamageVar(3 + bonus, ValueProp.Move), owner, null, null);
            }
        });

    // C#【攻技攻攻】对随机敌人造成13点伤害
    AddChord("C#", ChordCategory.Major,
        new[] { CardType.Attack, CardType.Skill, CardType.Attack, CardType.Attack },
        "CUTE_SAKIKO_MOD_C#CHORD.title", "CUTE_SAKIKO_MOD_C#CHORD.description", "c_sharp_chord",
        new[] { 13 },
        async (ctx, owner, bonus) =>
        {
            var combat = owner.CombatState; if (combat == null) return;
            var enemies = combat.HittableEnemies.OrderBy(e => e.Monster?.Id.Entry).ToList();
            if (enemies.Any())
            {
                var target = combat.RunState.Rng.CombatCardSelection.NextItem(enemies);
                await CreatureCmd.Damage(ctx, target, new DamageVar(13 + bonus, ValueProp.Move), owner, null, null);
            }
        });

    // D#【技攻攻攻】对随机敌人造成其最大生命值 (5+bonus)% 的伤害
    AddChord("D#", ChordCategory.Major,
        new[] { CardType.Skill, CardType.Attack, CardType.Attack, CardType.Attack },
        "CUTE_SAKIKO_MOD_D#CHORD.title", "CUTE_SAKIKO_MOD_D#CHORD.description", "d_sharp_chord",
        new[] { 5 }, // BaseValues[0] = 5，用于计算百分比
        async (ctx, owner, bonus) =>
        {
            var combat = owner.CombatState; if (combat == null) return;
            var enemies = combat.HittableEnemies.OrderBy(e => e.Monster?.Id.Entry).ToList();
            if (enemies.Any())
            {
                var target = combat.RunState.Rng.CombatCardSelection.NextItem(enemies);
                int percent = 5 + bonus;
                int dmg = (int)(target.MaxHp * percent / 100.0);
                if (dmg < 1) dmg = 1;
                await CreatureCmd.Damage(ctx, target, new DamageVar(dmg, ValueProp.Unblockable), owner, null, null);
            }
        });

    // E#【能攻攻攻】所有友方获得2层力量
    AddChord("E#", ChordCategory.Major,
        new[] { CardType.Power, CardType.Attack, CardType.Attack, CardType.Attack },
        "CUTE_SAKIKO_MOD_E#CHORD.title", "CUTE_SAKIKO_MOD_E#CHORD.description", "e_sharp_chord",
        new[] { 2 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<StrengthPower>(ctx, ally, 2 + bonus, owner, null);
        });

    // F#【攻攻攻能】所有友方获得1层仪式（RitualPower）
    AddChord("F#", ChordCategory.Major,
        new[] { CardType.Attack, CardType.Attack, CardType.Attack, CardType.Power },
        "CUTE_SAKIKO_MOD_F#CHORD.title", "CUTE_SAKIKO_MOD_F#CHORD.description", "f_sharp_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<RitualPower>(ctx, ally, 1 + bonus, owner, null);
        });

    // G#【技攻】在所有友方手牌中生成 1+bonus 张随机攻击牌（不重复）
    AddChord("G#", ChordCategory.Major,
        new[] { CardType.Skill, CardType.Attack },
        "CUTE_SAKIKO_MOD_G#CHORD.title", "CUTE_SAKIKO_MOD_G#CHORD.description", "g_sharp_chord",
        new[] { 1 }, // 基础数量 1
        async (ctx, owner, bonus) =>
        {
            foreach (var player in owner.CombatState?.Players ?? Enumerable.Empty<Player>())
            {
                int count = 1 + bonus;
                var cards = CardFactory.GetDistinctForCombat(player,
                    player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                        .Where(c => c.Type == CardType.Attack),
                    count,
                    player.RunState.Rng.CombatCardGeneration);
                foreach (var card in cards)
                    await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
            }
        });

    // A#【攻技】所有友方下一张攻击牌费用减1
    AddChord("A#", ChordCategory.Major,
        new[] { CardType.Attack, CardType.Skill },
        "CUTE_SAKIKO_MOD_A#CHORD.title", "CUTE_SAKIKO_MOD_A#CHORD.description", "a_sharp_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<ChordNextAttackCostReductionPower>(ctx, ally, 1 + bonus, owner, null);
        });

    // B#【攻攻】所有友方获得2层活力
    AddChord("B#", ChordCategory.Major,
        new[] { CardType.Attack, CardType.Attack },
        "CUTE_SAKIKO_MOD_B#CHORD.title", "CUTE_SAKIKO_MOD_B#CHORD.description", "b_sharp_chord",
        new[] { 2 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<VigorPower>(ctx, ally, 2 + bonus, owner, null);
        });

    // ==================== 小三和弦 ====================
    // Cm【技技攻】所有友方获得5点格挡
    AddChord("Cm", ChordCategory.Minor,
        new[] { CardType.Skill, CardType.Skill, CardType.Attack },
        "CUTE_SAKIKO_MOD_CMCHORD.title", "CUTE_SAKIKO_MOD_CMCHORD.description", "cm_chord",
        new[] { 5 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await CreatureCmd.GainBlock(ally, 5 + bonus, 0, null);
        });

    // Dm【技攻技】所有友方获得1层残影
    AddChord("Dm", ChordCategory.Minor,
        new[] { CardType.Skill, CardType.Attack, CardType.Skill },
        "CUTE_SAKIKO_MOD_DMCHORD.title", "CUTE_SAKIKO_MOD_DMCHORD.description", "dm_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<BlurPower>(ctx, ally, 1 + bonus, owner, null);
        });

    // Em【攻技技】所有友方本回合获得2层临时敏捷
    AddChord("Em", ChordCategory.Minor,
        new[] { CardType.Attack, CardType.Skill, CardType.Skill },
        "CUTE_SAKIKO_MOD_EMCHORD.title", "CUTE_SAKIKO_MOD_EMCHORD.description", "em_chord",
        new[] { 2 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<ChordTempDexterityPower>(ctx, ally, 2 + bonus, owner, null);
        });

    // Fm【技技技】所有敌方获得1层虚弱
    AddChord("Fm", ChordCategory.Minor,
        new[] { CardType.Skill, CardType.Skill, CardType.Skill },
        "CUTE_SAKIKO_MOD_FMCHORD.title", "CUTE_SAKIKO_MOD_FMCHORD.description", "fm_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            var enemies = owner.CombatState?.Enemies;
            if (enemies != null)
                foreach (var enemy in enemies)
                    await PowerCmd.Apply<WeakPower>(ctx, enemy, 1 + bonus, owner, null);
        });

    // Gm【技技技技】所有友方回复3点血量（保留复活逻辑）
    AddChord("Gm", ChordCategory.Minor,
        new[] { CardType.Skill, CardType.Skill, CardType.Skill, CardType.Skill },
        "CUTE_SAKIKO_MOD_GMCHORD.title", "CUTE_SAKIKO_MOD_GMCHORD.description", "gm_chord",
        new[] { 3 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            var combatState = owner.CombatState as CombatState;
            foreach (var ally in allies)
            {
                var wasDead = ally.IsDead;
                await CreatureCmd.Heal(ally, 3 + bonus);
                if (wasDead && ally.IsAlive && ally.Player != null && combatState != null)
                {
                    var player = ally.Player;
                    var drawPile = PileType.Draw.GetPile(player);
                    if (drawPile != null && drawPile.Cards.Count == 0)
                    {
                        var rng = combatState.RunState.Rng.Shuffle;
                        foreach (var deckCard in player.Deck.Cards)
                        {
                            var canonical = ModelDb.GetById<CardModel>(deckCard.Id);
                            if (canonical == null) continue;
                            var combatCard = combatState.CreateCard(canonical, player);
                            drawPile.AddInternal(combatCard);
                        }
                        drawPile.RandomizeOrderInternal(player, rng, combatState);
                    }
                    // 复活后给予额外资源
                    await PlayerCmd.GainEnergy(3, player);
                    await CardPileCmd.Draw(ctx, 5, player);
                    await CreatureCmd.TriggerAnim(ally, "idle_loop", 0f);
                }
            }
        });

    // Am【技技技攻】所有友方获得2层覆甲
    AddChord("Am", ChordCategory.Minor,
        new[] { CardType.Skill, CardType.Skill, CardType.Skill, CardType.Attack },
        "CUTE_SAKIKO_MOD_AMCHORD.title", "CUTE_SAKIKO_MOD_AMCHORD.description", "am_chord",
        new[] { 2 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<PlatingPower>(ctx, ally, 2 + bonus, owner, null);
        });

    // Bm【技技攻技】所有友方获得2层再生
    AddChord("Bm", ChordCategory.Minor,
        new[] { CardType.Skill, CardType.Skill, CardType.Attack, CardType.Skill },
        "CUTE_SAKIKO_MOD_BMCHORD.title", "CUTE_SAKIKO_MOD_BMCHORD.description", "bm_chord",
        new[] { 2 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<RegenPower>(ctx, ally, 2 + bonus, owner, null);
        });

    // C#m【技攻技技】所有友方获得10点格挡
    AddChord("C#m", ChordCategory.Minor,
        new[] { CardType.Skill, CardType.Attack, CardType.Skill, CardType.Skill },
        "CUTE_SAKIKO_MOD_C#MCHORD.title", "CUTE_SAKIKO_MOD_C#MCHORD.description", "c_sharp_m_chord",
        new[] { 10 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await CreatureCmd.GainBlock(ally, 10 + bonus, 0, null);
        });

    // D#m【攻技技技】所有友方获得1层缓冲 BufferPower
    AddChord("D#m", ChordCategory.Minor,
        new[] { CardType.Attack, CardType.Skill, CardType.Skill, CardType.Skill },
        "CUTE_SAKIKO_MOD_D#MCHORD.title", "CUTE_SAKIKO_MOD_D#MCHORD.description", "d_sharp_m_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<BufferPower>(ctx, ally, 1 + bonus, owner, null);
        });

    // E#m【能技技技】所有友方获得2层敏捷
    AddChord("E#m", ChordCategory.Minor,
        new[] { CardType.Power, CardType.Skill, CardType.Skill, CardType.Skill },
        "CUTE_SAKIKO_MOD_E#MCHORD.title", "CUTE_SAKIKO_MOD_E#MCHORD.description", "e_sharp_m_chord",
        new[] { 2 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<DexterityPower>(ctx, ally, 2 + bonus, owner, null);
        });

    // F#m【技技技能】所有友方获得1层壁垒
    AddChord("F#m", ChordCategory.Minor,
        new[] { CardType.Skill, CardType.Skill, CardType.Skill, CardType.Power },
        "CUTE_SAKIKO_MOD_F#MCHORD.title", "CUTE_SAKIKO_MOD_F#MCHORD.description", "f_sharp_m_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<BarricadePower>(ctx, ally, 1 + bonus, owner, null);
        });

    // G#m【技攻】在所有友方手牌中生成 1+bonus 张随机技能牌（不重复）
    AddChord("G#m", ChordCategory.Minor,
        new[] { CardType.Skill, CardType.Attack },
        "CUTE_SAKIKO_MOD_G#MCHORD.title", "CUTE_SAKIKO_MOD_G#MCHORD.description", "g_sharp_m_chord",
        new[] { 1 }, // 基础数量 1
        async (ctx, owner, bonus) =>
        {
            foreach (var player in owner.CombatState?.Players ?? Enumerable.Empty<Player>())
            {
                int count = 1 + bonus;
                var cards = CardFactory.GetDistinctForCombat(player,
                    player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                        .Where(c => c.Type == CardType.Skill),
                    count,
                    player.RunState.Rng.CombatCardGeneration);
                foreach (var card in cards)
                    await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
            }
        });

    // A#m【攻技】所有友方下一张技能牌费用减1
    AddChord("A#m", ChordCategory.Minor,
        new[] { CardType.Attack, CardType.Skill },
        "CUTE_SAKIKO_MOD_A#MCHORD.title", "CUTE_SAKIKO_MOD_A#MCHORD.description", "a_sharp_m_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<ChordNextSkillCostReductionPower>(ctx, ally, 1 + bonus, owner, null);
        });

    // B#m【技技】所有友方获得1层覆甲
    AddChord("B#m", ChordCategory.Minor,
        new[] { CardType.Skill, CardType.Skill },
        "CUTE_SAKIKO_MOD_B#MCHORD.title", "CUTE_SAKIKO_MOD_B#MCHORD.description", "b_sharp_m_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<PlatingPower>(ctx, ally, 1 + bonus, owner, null);
        });

    // ==================== 属七和弦 ====================
    // C7【攻技攻】所有敌人本回合减少3层力量（沿用旧 ChordTempStrengthDownPower，可改用新临时力量逻辑）
    AddChord("C7", ChordCategory.Dominant,
        new[] { CardType.Attack, CardType.Skill, CardType.Attack },
        "CUTE_SAKIKO_MOD_C7CHORD.title", "CUTE_SAKIKO_MOD_C7CHORD.description", "c7_chord",
        new[] { 3 },
        async (ctx, owner, bonus) =>
        {
            var enemies = owner.CombatState?.Enemies;
            if (enemies != null)
                foreach (var enemy in enemies)
                    await PowerCmd.Apply<ChordTempStrengthDownPower>(ctx, enemy, 3 + bonus, owner, null);
        });

    // D7【技攻技】所有敌人获得3层中毒
    AddChord("D7", ChordCategory.Dominant,
        new[] { CardType.Skill, CardType.Attack, CardType.Skill },
        "CUTE_SAKIKO_MOD_D7CHORD.title", "CUTE_SAKIKO_MOD_D7CHORD.description", "d7_chord",
        new[] { 3 },
        async (ctx, owner, bonus) =>
        {
            var enemies = owner.CombatState?.Enemies;
            if (enemies != null)
                foreach (var enemy in enemies)
                    await PowerCmd.Apply<PoisonPower>(ctx, enemy, 3 + bonus, owner, null);
        });

    // E7【攻攻技】所有敌人获得5层灾厄 DoomPower
    AddChord("E7", ChordCategory.Dominant,
        new[] { CardType.Attack, CardType.Attack, CardType.Skill },
        "CUTE_SAKIKO_MOD_E7CHORD.title", "CUTE_SAKIKO_MOD_E7CHORD.description", "e7_chord",
        new[] { 5 },
        async (ctx, owner, bonus) =>
        {
            var enemies = owner.CombatState?.Enemies;
            if (enemies != null)
                foreach (var enemy in enemies)
                    await PowerCmd.Apply<DoomPower>(ctx, enemy, 5 + bonus, owner, null);
        });

    // F7【技技攻】所有敌人获得1层易伤和虚弱
    AddChord("F7", ChordCategory.Dominant,
        new[] { CardType.Skill, CardType.Skill, CardType.Attack },
        "CUTE_SAKIKO_MOD_F7CHORD.title", "CUTE_SAKIKO_MOD_F7CHORD.description", "f7_chord",
        new[] { 1, 1 },
        async (ctx, owner, bonus) =>
        {
            var enemies = owner.CombatState?.Enemies;
            if (enemies != null)
                foreach (var enemy in enemies)
                {
                    await PowerCmd.Apply<VulnerablePower>(ctx, enemy, 1 + bonus, owner, null);
                    await PowerCmd.Apply<WeakPower>(ctx, enemy, 1 + bonus, owner, null);
                }
        });
    
    // G7【攻攻技技】所有友方获得 (1+bonus) 层“和弦之力：消耗”
    AddChord("G7", ChordCategory.Dominant,
        new[] { CardType.Attack, CardType.Attack, CardType.Skill, CardType.Skill },
        "CUTE_SAKIKO_MOD_G7CHORD.title", "CUTE_SAKIKO_MOD_G7CHORD.description", "g7_chord",
        new[] { 1 }, // 基础层数 1
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<ChordEndOfTurnExhaustPower>(ctx, ally, 1 + bonus, owner, null);
        });

    // A7【攻技攻技】所有友方保留手牌1回合（RetainHandPower）
    AddChord("A7", ChordCategory.Dominant,
        new[] { CardType.Attack, CardType.Skill, CardType.Attack, CardType.Skill },
        "CUTE_SAKIKO_MOD_A7CHORD.title", "CUTE_SAKIKO_MOD_A7CHORD.description", "a7_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<RetainHandPower>(ctx, ally, 1 + bonus, owner, null);
        });

    // B7【攻技技攻】所有友方下一张打出的牌会多打出1次（DuplicationPower）
    AddChord("B7", ChordCategory.Dominant,
        new[] { CardType.Attack, CardType.Skill, CardType.Skill, CardType.Attack },
        "CUTE_SAKIKO_MOD_B7CHORD.title", "CUTE_SAKIKO_MOD_B7CHORD.description", "b7_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<DuplicationPower>(ctx, ally, 1 + bonus, owner, null);
        });

    // C#7【技攻技攻】所有敌人本回合减少5点力量（ChordTempStrengthDownPower）
    AddChord("C#7", ChordCategory.Dominant,
        new[] { CardType.Skill, CardType.Attack, CardType.Skill, CardType.Attack },
        "CUTE_SAKIKO_MOD_C#7CHORD.title", "CUTE_SAKIKO_MOD_C#7CHORD.description", "c_sharp_7_chord",
        new[] { 5 },
        async (ctx, owner, bonus) =>
        {
            var enemies = owner.CombatState?.Enemies;
            if (enemies != null)
                foreach (var enemy in enemies)
                    await PowerCmd.Apply<ChordTempStrengthDownPower>(ctx, enemy, 5 + bonus, owner, null);
        });

    // D#7【技技攻攻】所有敌人获得1层摧残 DebilitatePower
    AddChord("D#7", ChordCategory.Dominant,
        new[] { CardType.Skill, CardType.Skill, CardType.Attack, CardType.Attack },
        "CUTE_SAKIKO_MOD_D#7CHORD.title", "CUTE_SAKIKO_MOD_D#7CHORD.description", "d_sharp_7_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            var enemies = owner.CombatState?.Enemies;
            if (enemies != null)
                foreach (var enemy in enemies)
                    await PowerCmd.Apply<DebilitatePower>(ctx, enemy, 1 + bonus, owner, null);
        });

    // E#7【能攻攻技】清除所有敌人的力量，然后所有友方获得等量力量
    AddChord("E#7", ChordCategory.Dominant,
        new[] { CardType.Power, CardType.Attack, CardType.Attack, CardType.Skill },
        "CUTE_SAKIKO_MOD_E#7CHORD.title", "CUTE_SAKIKO_MOD_E#7CHORD.description", "e_sharp_7_chord",
        new int[0],
        async (ctx, owner, bonus) =>
        {
            var enemies = owner.CombatState?.Enemies;
            int totalStr = 0;
            if (enemies != null)
                foreach (var enemy in enemies)
                {
                    var strPower = enemy.Powers.OfType<StrengthPower>().FirstOrDefault();
                    if (strPower != null)
                    {
                        totalStr += strPower.Amount;
                        strPower.RemoveInternal();
                    }
                }
            if (totalStr > 0)
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                    await PowerCmd.Apply<StrengthPower>(ctx, ally, totalStr + bonus, owner, null);
            }
        });

    // F#7【能技技攻】在所有友方手牌中生成 1+bonus 张免费随机能力牌（不重复）
    AddChord("F#7", ChordCategory.Dominant,
        new[] { CardType.Power, CardType.Skill, CardType.Skill, CardType.Attack },
        "CUTE_SAKIKO_MOD_F#7CHORD.title", "CUTE_SAKIKO_MOD_F#7CHORD.description", "f_sharp_7_chord",
        new[] { 1 }, // 基础数量 1
        async (ctx, owner, bonus) =>
        {
            foreach (var player in owner.CombatState?.Players ?? Enumerable.Empty<Player>())
            {
                int count = 1 + bonus;
                var cards = CardFactory.GetDistinctForCombat(player,
                    player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                        .Where(c => c.Type == CardType.Power),
                    count,
                    player.RunState.Rng.CombatCardGeneration);
                foreach (var card in cards)
                {
                    card.SetToFreeThisTurn();
                    await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
                }
            }
        });

    // G#7【技技】所有友方抽1张牌
    AddChord("G#7", ChordCategory.Dominant,
        new[] { CardType.Skill, CardType.Skill },
        "CUTE_SAKIKO_MOD_G#7CHORD.title", "CUTE_SAKIKO_MOD_G#7CHORD.description", "g_sharp_7_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            foreach (var player in owner.CombatState?.Players ?? Enumerable.Empty<Player>())
                await CardPileCmd.Draw(ctx, 1 + bonus, player);
        });

    // A#7【攻攻】所有友方获得1点能量
    AddChord("A#7", ChordCategory.Dominant,
        new[] { CardType.Attack, CardType.Attack },
        "CUTE_SAKIKO_MOD_A#7CHORD.title", "CUTE_SAKIKO_MOD_A#7CHORD.description", "a_sharp_7_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            foreach (var player in owner.CombatState?.Players ?? Enumerable.Empty<Player>())
                await PlayerCmd.GainEnergy(1 + bonus, player);
        });

    // B#7【技攻】下一个和弦演奏的数值增加1（ChordBonusPower）
    AddChord("B#7", ChordCategory.Dominant,
        new[] { CardType.Skill, CardType.Attack },
        "CUTE_SAKIKO_MOD_B#7CHORD.title", "CUTE_SAKIKO_MOD_B#7CHORD.description", "b_sharp_7_chord",
        new[] { 1 },
        async (ctx, owner, bonus) =>
        {
            var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
            foreach (var ally in allies)
                await PowerCmd.Apply<ChordBonusPower>(ctx, ally, 1 + bonus, owner, null);
        });

            // ========== Anon 临时和弦 (已改为 bonus 加成) ==========
        AddTemporaryChord("AnonCChord", ChordCategory.Anon,
            new[] { CardType.Skill, CardType.Skill, CardType.Skill },
            "CUTE_SAKIKO_MOD_ANONCCHORD.title", "CUTE_SAKIKO_MOD_ANONCCHORD.description", "anon_c_chord",
            new[] { 1 }, // 基础升级张数
            async (ctx, owner, bonus) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                var rng = combat.RunState.Rng.CombatCardSelection;
                var upgradedCards = new List<CardModel>();

                foreach (var player in combat.Players)
                {
                    var pcs = player.PlayerCombatState;
                    if (pcs == null) continue;

                    var allCards = (pcs.Hand?.Cards ?? Enumerable.Empty<CardModel>())
                        .Concat(pcs.DrawPile?.Cards ?? Enumerable.Empty<CardModel>())
                        .Concat(pcs.DiscardPile?.Cards ?? Enumerable.Empty<CardModel>());

                    var upgradable = allCards.Where(c => c.IsUpgradable).ToList();
                    if (upgradable.Count == 0) continue;

                    // 升级 1 + bonus 张牌（每人，不够则全升）
                    var count = 1 + bonus;
                    var chosen = upgradable
                        .OrderBy(_ => rng.NextInt())
                        .Take(count)
                        .ToList();

                    foreach (var card in chosen)
                    {
                        CardCmd.Upgrade(card);
                        upgradedCards.Add(card);
                    }
                }

                if (upgradedCards.Count > 0)
                {
                    CardCmd.Preview(upgradedCards, 0.5f);
                    await Cmd.CustomScaledWait(0.1f, 0.2f);
                }
            });

        AddTemporaryChord("AnonDChord", ChordCategory.Anon,
            new[] { CardType.Skill, CardType.Attack, CardType.Attack, CardType.Attack },
            "CUTE_SAKIKO_MOD_ANONDCHORD.title", "CUTE_SAKIKO_MOD_ANONDCHORD.description", "anon_d_chord",
            new int[0],
            async (ctx, owner, bonus) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                var allies = combat.Players.ToList();
                foreach (var player in allies)
                {
                    // 每个队友获得 1 + bonus 瓶随机药水
                    for (int i = 0; i < 1 + bonus; i++)
                    {
                        var randomPotion = PotionFactory.CreateRandomPotionInCombat(
                            player,
                            player.RunState.Rng.CombatPotionGeneration
                        ).ToMutable();
                        await PotionCmd.TryToProcure(randomPotion, player);
                    }
                }
            });

        AddTemporaryChord("AnonEChord", ChordCategory.Anon,
            new[] { CardType.Skill, CardType.Attack, CardType.Skill, CardType.Skill },
            "CUTE_SAKIKO_MOD_ANONECHORD.title", "CUTE_SAKIKO_MOD_ANONECHORD.description", "anon_e_chord",
            new[] { 1, 4 }, // 基础残影层数, 格挡
            async (ctx, owner, bonus) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                var allies = combat.Players.Select(p => p.Creature).ToList();
                foreach (var ally in allies)
                {
                    await PowerCmd.Apply<BlurPower>(ctx, ally, 1 + bonus, owner, null);
                    await CreatureCmd.GainBlock(ally, 4 + bonus, 0, null);
                }
            });

        AddTemporaryChord("AnonFChord", ChordCategory.Anon,
            new[] { CardType.Attack, CardType.Attack, CardType.Attack, CardType.Attack },
            "CUTE_SAKIKO_MOD_ANONFCHORD.title", "CUTE_SAKIKO_MOD_ANONFCHORD.description", "anon_f_chord",
            new[] { 20, 1 }, // 伤害, 虚弱
            async (ctx, owner, bonus) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;
                var enemies = combat.Enemies;
                if (enemies != null && enemies.Any())
                    await CreatureCmd.Damage(ctx, enemies, new DamageVar(20 + bonus, ValueProp.Move), owner, null, null);
                var allies = combat.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                {
                    var weak2 = await PowerCmd.Apply<WeakPower>(ctx, ally, 1 + bonus, owner, null);
                    if (weak2 != null) weak2.SkipNextDurationTick = false;
                }
            });

        AddTemporaryChord("AnonGChord", ChordCategory.Anon,
            new[] { CardType.Skill, CardType.Skill, CardType.Attack, CardType.Attack },
            "CUTE_SAKIKO_MOD_ANONGCHORD.title", "CUTE_SAKIKO_MOD_ANONGCHORD.description", "anon_g_chord",
            new[] { 1, 1 }, // 抽牌, 能量
            async (ctx, owner, bonus) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                foreach (var player in combat.Players)
                {
                    if (player == null) continue;
                    await CardPileCmd.Draw(ctx, 1 + bonus, player);
                    await PlayerCmd.GainEnergy(1 + bonus, player);
                }
            });

        AddTemporaryChord("GreyAnonChord", ChordCategory.Anon,
            new[] { CardType.Status, CardType.Attack, CardType.Attack },
            "CUTE_SAKIKO_MOD_GREYANONCHORD.title", "CUTE_SAKIKO_MOD_GREYANONCHORD.description", "grey_anon_chord",
            new[] { 2, 1, 1 }, // 自伤, 抽牌, 能量
            async (ctx, owner, bonus) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                // 固定自伤不加成（避免自杀），抽牌和能量受加成
                await CreatureCmd.Damage(
                    ctx,
                    owner,
                    new DamageVar(2, ValueProp.Unblockable | ValueProp.Unpowered),
                    owner,
                    (CardModel?)null,
                    (CardPlay?)null
                );

                foreach (var player in combat.Players)
                {
                    if (player == null) continue;
                    await CardPileCmd.Draw(ctx, 1 + bonus, player);
                    await PlayerCmd.GainEnergy(1 + bonus, player);
                }
            });
        
        AddTemporaryChord("HekitenbansouChord", ChordCategory.Anon,
            new[] { CardType.Attack, CardType.Skill, CardType.Attack, CardType.Skill },
            "CUTE_SAKIKO_MOD_HEKITENBANSOUCHORD.title", "CUTE_SAKIKO_MOD_HEKITENBANSOUCHORD.description",
            "hekitenbansou_chord",
            new[] { 1 }, // 转化层数
            async (ctx, owner, bonus) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                var allies = combat.Players.Select(p => p.Creature).ToList();

                foreach (var ally in allies)
                {
                    var frail = ally.Powers.OfType<FrailPower>().FirstOrDefault();
                    if (frail != null)
                    {
                        frail.RemoveInternal();
                        await PowerCmd.Apply<DexterityPower>(ctx, ally, 1 + bonus, owner, null);
                    }

                    var weak = ally.Powers.OfType<WeakPower>().FirstOrDefault();
                    if (weak != null)
                    {
                        weak.RemoveInternal();
                        await PowerCmd.Apply<StrengthPower>(ctx, ally, 1 + bonus, owner, null);
                    }
                }
            });
}

    public static List<string> GetTemporaryChordIds(ChordCategory? category = null)
    {
        var query = _temporaryChordIds.Where(id => AllChords[id].IsTemporaryOnly);
        if (category.HasValue)
            query = query.Where(id => AllChords[id].Category == category.Value);
        return query.ToList();
    }

    public static List<string> GetLearnableChordIds(ChordCategory category)
    {
        if (category == ChordCategory.Anon)
            return new List<string>();

        var exclude = category switch
        {
            ChordCategory.Major => new[] { "C" },
            ChordCategory.Minor => new[] { "Cm" },
            ChordCategory.Dominant => new[] { "C7" },
            _ => Array.Empty<string>()
        };
        return AllChords.Values
            .Where(c => c.Category == category
                        && !exclude.Contains(c.Id)
                        && !c.IsTemporaryOnly)
            .Select(c => c.Id)
            .ToList();
    }

    public static string GetBaseChordId(ChordCategory category)
    {
        return category switch
        {
            ChordCategory.Major => "C",
            ChordCategory.Minor => "Am",
            ChordCategory.Dominant => "G7",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static bool MatchesChord(IReadOnlyList<CardType> pattern, IReadOnlyList<CardType> sequence)
    {
        if (sequence.Count < pattern.Count) return false;
        for (var i = 0; i < pattern.Count; i++)
        {
            var expected = pattern[i];
            var actual = sequence[sequence.Count - pattern.Count + i];

            // 通配：任意音符
            if (expected == Entry.AnyNote)
                continue;

            if (expected == CardType.Status)
            {
                if (actual == CardType.Attack || actual == CardType.Skill || actual == CardType.Power)
                    return false;
            }
            else if (expected != actual)
            {
                return false;
            }
        }
        return true;
    }
}