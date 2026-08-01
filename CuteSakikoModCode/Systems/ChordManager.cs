using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

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

    private static void RegisterChords()
    {
        // ========== 大三和弦 ==========
        AddChord("C", ChordCategory.Major,
            new[] { CardType.Attack, CardType.Attack, CardType.Skill },
            "CUTE_SAKIKO_MOD_CCHORD.title", "CUTE_SAKIKO_MOD_CCHORD.description", "c_chord",
            new[] { 3, 3 },
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                var enemies = combat.HittableEnemies
                    .OrderBy(e => e.Monster?.Id.Entry)
                    .ToList();

                if (enemies.Any())
                {
                    var target = combat.RunState.Rng.CombatCardSelection.NextItem(enemies);
                    await CreatureCmd.Damage(ctx, target, new DamageVar(3 * mult, ValueProp.Move), owner, null, null);
                }

                await CreatureCmd.GainBlock(owner, 3 * mult, 0, null);
            });

        AddChord("G", ChordCategory.Major,
            new[] { CardType.Attack, CardType.Attack, CardType.Attack, CardType.Attack },
            "CUTE_SAKIKO_MOD_GCHORD.title", "CUTE_SAKIKO_MOD_GCHORD.description", "g_chord",
            new[] { 3 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                    await PowerCmd.Apply<VigorPower>(ctx, ally, 3 * mult, owner, null);
            });

        AddChord("D", ChordCategory.Major,
            new[] { CardType.Skill, CardType.Attack, CardType.Attack, CardType.Attack },
            "CUTE_SAKIKO_MOD_DCHORD.title", "CUTE_SAKIKO_MOD_DCHORD.description", "d_chord",
            new[] { 1, 2 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                {
                    var frail = await PowerCmd.Apply<FrailPower>(ctx, ally, 1 * mult, owner, null);
                    if (frail != null) frail.SkipNextDurationTick = false;
                    await PowerCmd.Apply<StrengthPower>(ctx, ally, 2 * mult, owner, null);
                }
            });

        AddChord("A", ChordCategory.Major,
            new[] { CardType.Attack, CardType.Skill, CardType.Skill },
            "CUTE_SAKIKO_MOD_ACHORD.title", "CUTE_SAKIKO_MOD_ACHORD.description", "a_chord",
            new[] { 6 },
            async (ctx, owner, mult) =>
            {
                var enemies = owner.CombatState?.Enemies;
                if (enemies != null)
                    await CreatureCmd.Damage(ctx, enemies, new DamageVar(6 * mult, ValueProp.Move), owner, null, null);
            });

        AddChord("E", ChordCategory.Major,
            new[] { CardType.Power, CardType.Attack, CardType.Skill },
            "CUTE_SAKIKO_MOD_ECHORD.title", "CUTE_SAKIKO_MOD_ECHORD.description", "e_chord",
            new[] { 1 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                    await PowerCmd.Apply<StrengthPower>(ctx, ally, 1 * mult, owner, null);
            });

        AddChord("C#", ChordCategory.Major,
            new[] { CardType.Attack, CardType.Attack },
            "CUTE_SAKIKO_MOD_C#CHORD.title", "CUTE_SAKIKO_MOD_C#CHORD.description", "c_sharp_chord",
            new[] { 1 },
            async (ctx, owner, mult) => { await PowerCmd.Apply<VigorPower>(ctx, owner, 1 * mult, owner, null); });

        AddChord("D#", ChordCategory.Major,
            new[] { CardType.Skill, CardType.Attack },
            "CUTE_SAKIKO_MOD_D#CHORD.title", "CUTE_SAKIKO_MOD_D#CHORD.description", "d_sharp_chord",
            new[] { 3 },
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                var enemies = combat.HittableEnemies
                    .OrderBy(e => e.Monster?.Id.Entry)
                    .ToList();

                if (enemies.Any())
                {
                    var target = combat.RunState.Rng.CombatCardSelection.NextItem(enemies);
                    await CreatureCmd.Damage(ctx, target, new DamageVar(3 * mult, ValueProp.Move), owner, null, null);
                }
            });

        // ========== 小三和弦 ==========
        AddChord("Am", ChordCategory.Minor,
            new[] { CardType.Skill, CardType.Skill, CardType.Attack },
            "CUTE_SAKIKO_MOD_AMCHORD.title", "CUTE_SAKIKO_MOD_AMCHORD.description", "am_chord",
            new[] { 4 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                    await CreatureCmd.GainBlock(ally, 4 * mult, 0, null);
            });

        AddChord("Gm", ChordCategory.Minor,
            new[] { CardType.Skill, CardType.Skill, CardType.Skill, CardType.Skill },
            "CUTE_SAKIKO_MOD_GMCHORD.title", "CUTE_SAKIKO_MOD_GMCHORD.description", "gm_chord",
            new[] { 3 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                var combatState = owner.CombatState as CombatState;
                foreach (var ally in allies)
                {
                    var wasDead = ally.IsDead;
                    await CreatureCmd.Heal(ally, 3 * mult);

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

                        await CreatureCmd.TriggerAnim(ally, "idle_loop", 0f);
                    }
                }
            });

        AddChord("Em", ChordCategory.Minor,
            new[] { CardType.Skill, CardType.Skill, CardType.Attack, CardType.Skill },
            "CUTE_SAKIKO_MOD_EMCHORD.title", "CUTE_SAKIKO_MOD_EMCHORD.description", "em_chord",
            new[] { 1 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                    await PowerCmd.Apply<ReflectPower>(ctx, ally, 1 * mult, owner, null);
            });

        AddChord("Dm", ChordCategory.Minor,
            new[] { CardType.Skill, CardType.Attack, CardType.Skill },
            "CUTE_SAKIKO_MOD_DMCHORD.title", "CUTE_SAKIKO_MOD_DMCHORD.description", "dm_chord",
            new[] { 1 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                    await PowerCmd.Apply<RegenPower>(ctx, ally, 1 * mult, owner, null);
            });

        AddChord("Bm", ChordCategory.Minor,
            new[] { CardType.Power, CardType.Skill, CardType.Skill },
            "CUTE_SAKIKO_MOD_BMCHORD.title", "CUTE_SAKIKO_MOD_BMCHORD.description", "bm_chord",
            new[] { 1 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                    await PowerCmd.Apply<DexterityPower>(ctx, ally, 1 * mult, owner, null);
            });

        AddChord("C#m", ChordCategory.Minor,
            new[] { CardType.Skill, CardType.Skill },
            "CUTE_SAKIKO_MOD_C#MCHORD.title", "CUTE_SAKIKO_MOD_C#MCHORD.description", "c_sharp_m_chord",
            new[] { 2 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                    await CreatureCmd.GainBlock(ally, 2 * mult, 0, null);
            });

        AddChord("D#m", ChordCategory.Minor,
            new[] { CardType.Attack, CardType.Attack },
            "CUTE_SAKIKO_MOD_D#MCHORD.title", "CUTE_SAKIKO_MOD_D#MCHORD.description", "d_sharp_m_chord",
            new[] { 1 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                    await PowerCmd.Apply<PlatingPower>(ctx, ally, 1 * mult, owner, null);
            });

        AddChord("E#m", ChordCategory.Minor,
            new[] { CardType.Skill, CardType.Skill, CardType.Attack, CardType.Skill },
            "CUTE_SAKIKO_MOD_E#MCHORD.title", "CUTE_SAKIKO_MOD_E#MCHORD.description", "e_sharp_m_chord",
            new[] { 1, 8 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                {
                    var weak = await PowerCmd.Apply<WeakPower>(ctx, ally, 1 * mult, owner, null);
                    if (weak != null) weak.SkipNextDurationTick = false;
                    await CreatureCmd.GainBlock(ally, 8 * mult, 0, null);
                }
            });


        // ========== 属七和弦 ==========
        AddChord("G7", ChordCategory.Dominant,
            new[] { CardType.Attack, CardType.Skill, CardType.Attack },
            "CUTE_SAKIKO_MOD_G7CHORD.title", "CUTE_SAKIKO_MOD_G7CHORD.description", "g7_chord",
            new[] { 2 },
            async (ctx, owner, mult) =>
            {
                var enemies = owner.CombatState?.Enemies;
                if (enemies != null)
                    foreach (var enemy in enemies)
                        await PowerCmd.Apply<ChordTempStrengthDownPower>(ctx, enemy, 2 * mult, owner, null);
            });

        AddChord("D7", ChordCategory.Dominant,
            new[] { CardType.Skill, CardType.Skill, CardType.Attack },
            "CUTE_SAKIKO_MOD_D7CHORD.title", "CUTE_SAKIKO_MOD_D7CHORD.description", "d7_chord",
            new[] { 1 },
            async (ctx, owner, mult) =>
            {
                var enemies = owner.CombatState?.Enemies;
                if (enemies != null)
                    foreach (var enemy in enemies)
                        await PowerCmd.Apply<WeakPower>(ctx, enemy, 1 * mult, owner, null);
            });

        AddChord("A7", ChordCategory.Dominant,
            new[] { CardType.Power, CardType.Skill, CardType.Power, CardType.Skill },
            "CUTE_SAKIKO_MOD_A7CHORD.title", "CUTE_SAKIKO_MOD_A7CHORD.description", "a7_chord",
            new[] { 1 },
            async (ctx, owner, mult) =>
            {
                var enemies = owner.CombatState?.Enemies;
                if (enemies != null)
                {
                    foreach (var enemy in enemies.Where(e => e.IsAlive))
                    {
                        await CreatureCmd.Stun(enemy);
                    }
                }
            });

        AddChord("E7", ChordCategory.Dominant,
            new[] { CardType.Skill, CardType.Power, CardType.Skill, CardType.Power },
            "CUTE_SAKIKO_MOD_E7CHORD.title", "CUTE_SAKIKO_MOD_E7CHORD.description", "e7_chord",
            new[] { 1 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                    await PowerCmd.Apply<BarricadePower>(ctx, ally, 1 * mult, owner, null);
            });

        AddChord("C#7", ChordCategory.Dominant,
            new[] { CardType.Attack, CardType.Skill },
            "CUTE_SAKIKO_MOD_C#7CHORD.title", "CUTE_SAKIKO_MOD_C#7CHORD.description", "c_sharp_7_chord",
            new[] { 1 },
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat != null)
                    foreach (var player in combat.Players)
                        await PlayerCmd.GainEnergy(1 * mult, player);
            });

        AddChord("D#7", ChordCategory.Dominant,
            new[] { CardType.Skill, CardType.Attack },
            "CUTE_SAKIKO_MOD_D#7CHORD.title", "CUTE_SAKIKO_MOD_D#7CHORD.description", "d_sharp_7_chord",
            new[] { 1 },
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat != null)
                    foreach (var player in combat.Players)
                        await CardPileCmd.Draw(ctx, 1 * mult, player);
            });

        AddTemporaryChord("AnonCChord", ChordCategory.Anon,
            new[] { CardType.Skill, CardType.Skill, CardType.Skill },
            "CUTE_SAKIKO_MOD_ANONCCHORD.title", "CUTE_SAKIKO_MOD_ANONCCHORD.description", "anon_c_chord",
            new[] { 1 },
            async (ctx, owner, mult) =>
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

                    var chosen = upgradable
                        .OrderBy(_ => rng.NextInt())
                        .Take(mult)
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
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                var allies = combat.Players.ToList();
                foreach (var player in allies)
                {
                    var randomPotion = PotionFactory.CreateRandomPotionInCombat(
                        player,
                        player.RunState.Rng.CombatPotionGeneration
                    ).ToMutable();
                    await PotionCmd.TryToProcure(randomPotion, player);
                }
            });

        AddTemporaryChord("AnonEChord", ChordCategory.Anon,
            new[] { CardType.Skill, CardType.Attack, CardType.Skill, CardType.Skill },
            "CUTE_SAKIKO_MOD_ANONECHORD.title", "CUTE_SAKIKO_MOD_ANONECHORD.description", "anon_e_chord",
            new[] { 1, 4 },
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                var allies = combat.Players.Select(p => p.Creature).ToList();
                foreach (var ally in allies)
                {
                    await PowerCmd.Apply<BlurPower>(ctx, ally, 1 * mult, owner, null);
                    await CreatureCmd.GainBlock(ally, 4 * mult, 0, null);
                }
            });

        AddTemporaryChord("AnonFChord", ChordCategory.Anon,
            new[] { CardType.Attack, CardType.Attack, CardType.Attack, CardType.Attack },
            "CUTE_SAKIKO_MOD_ANONFCHORD.title", "CUTE_SAKIKO_MOD_ANONFCHORD.description", "anon_f_chord",
            new[] { 20, 1 },
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;
                var enemies = combat.Enemies;
                if (enemies != null && enemies.Any())
                    await CreatureCmd.Damage(ctx, enemies, new DamageVar(20 * mult, ValueProp.Move), owner, null, null);
                var allies = combat.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                {
                    var weak2 = await PowerCmd.Apply<WeakPower>(ctx, ally, 1 * mult, owner, null);
                    if (weak2 != null) weak2.SkipNextDurationTick = false;
                }
            });

        AddTemporaryChord("AnonGChord", ChordCategory.Anon,
            new[] { CardType.Skill, CardType.Skill, CardType.Attack, CardType.Attack },
            "CUTE_SAKIKO_MOD_ANONGCHORD.title", "CUTE_SAKIKO_MOD_ANONGCHORD.description", "anon_g_chord",
            new[] { 1, 1 },
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                foreach (var player in combat.Players)
                {
                    if (player == null) continue;
                    await CardPileCmd.Draw(ctx, 1 * mult, player);
                    await PlayerCmd.GainEnergy(1 * mult, player);
                }
            });

        AddTemporaryChord("GreyAnonChord", ChordCategory.Anon,
            new[] { CardType.Status, CardType.Attack, CardType.Attack },
            "CUTE_SAKIKO_MOD_GREYANONCHORD.title", "CUTE_SAKIKO_MOD_GREYANONCHORD.description", "grey_anon_chord",
            new[] { 2, 1, 1 },
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                await CreatureCmd.Damage(
                    ctx,
                    owner,
                    new DamageVar(2 * mult, ValueProp.Unblockable | ValueProp.Unpowered),
                    owner,
                    (CardModel?)null,
                    (CardPlay?)null
                );

                foreach (var player in combat.Players)
                {
                    if (player == null) continue;
                    await CardPileCmd.Draw(ctx, 1 * mult, player);
                    await PlayerCmd.GainEnergy(1 * mult, player);
                }
            });
        
        AddTemporaryChord("HekitenbansouChord", ChordCategory.Anon,
            new[] { CardType.Attack, CardType.Skill, CardType.Attack, CardType.Skill },
            "CUTE_SAKIKO_MOD_HEKITENBANSOUCHORD.title", "CUTE_SAKIKO_MOD_HEKITENBANSOUCHORD.description",
            "hekitenbansou_chord",
            new[] { 1 },
            async (ctx, owner, mult) =>
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
                        await PowerCmd.Apply<DexterityPower>(ctx, ally, 1 * mult, owner, null);
                    }

                    var weak = ally.Powers.OfType<WeakPower>().FirstOrDefault();
                    if (weak != null)
                    {
                        weak.RemoveInternal();
                        await PowerCmd.Apply<StrengthPower>(ctx, ally, 1 * mult, owner, null);
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
            ChordCategory.Minor => new[] { "Am" },
            ChordCategory.Dominant => new[] { "G7" },
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