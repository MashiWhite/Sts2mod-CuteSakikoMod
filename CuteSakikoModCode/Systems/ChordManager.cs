using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Systems
{
    public static class ChordManager
    {
        public static Dictionary<string, ChordDefinition> AllChords { get; } = new();

        static ChordManager()
        {
            RegisterChords();
        }

        private static void RegisterChords()
        {
            // ========== 大三和弦 ==========
            // 初始 C【攻 攻 技】随机造成3点伤害并获得3点格挡
            AddChord("C", ChordCategory.Major,
                new[] { CardType.Attack, CardType.Attack, CardType.Skill },
                "CUTESAKIKOMOD-CCHORD.title", "CUTESAKIKOMOD-CCHORD.description", "c_chord",
                new[] { 3, 3 },
                async (ctx, owner, mult) =>
                {
                    var combat = owner.CombatState;
                    if (combat == null) return;
                    var enemies = combat.HittableEnemies;
                    if (enemies.Any())
                    {
                        var target = combat.RunState.Rng.CombatCardSelection.NextItem(enemies);
                        await CreatureCmd.Damage(ctx, target, 3 * mult, ValueProp.Move, owner, null);
                    }
                    await CreatureCmd.GainBlock(owner, 3 * mult, (ValueProp)0, null);
                });

            // G【攻 攻 攻 攻】所有友方获得3点活力（数值已调整）
            AddChord("G", ChordCategory.Major,
                new[] { CardType.Attack, CardType.Attack, CardType.Attack, CardType.Attack },
                "CUTESAKIKOMOD-GCHORD.title", "CUTESAKIKOMOD-GCHORD.description", "g_chord",
                new[] { 3 },
                async (ctx, owner, mult) =>
                {
                    var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                    foreach (var ally in allies)
                        await PowerCmd.Apply<VigorPower>(ally, 3 * mult, owner, null);
                });

            // D【技 攻 攻 攻】所有友方本回合获得1层易伤和2点力量（数值已调整）
            AddChord("D", ChordCategory.Major,
                new[] { CardType.Skill, CardType.Attack, CardType.Attack, CardType.Attack },
                "CUTESAKIKOMOD-DCHORD.title", "CUTESAKIKOMOD-DCHORD.description", "d_chord",
                new[] { 1, 2 },
                async (ctx, owner, mult) =>
                {
                    var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                    foreach (var ally in allies)
                    {
                        await PowerCmd.Apply<VulnerablePower>(ally, 1 * mult, owner, null);
                        await PowerCmd.Apply<StrengthPower>(ally, 2 * mult, owner, null);
                    }
                });

            // A【攻 技 技】对所有敌人造成6点伤害
            AddChord("A", ChordCategory.Major,
                new[] { CardType.Attack, CardType.Skill, CardType.Skill },
                "CUTESAKIKOMOD-ACHORD.title", "CUTESAKIKOMOD-ACHORD.description", "a_chord",
                new[] { 6 },
                async (ctx, owner, mult) =>
                {
                    var enemies = owner.CombatState?.Enemies;
                    if (enemies != null)
                        await CreatureCmd.Damage(ctx, enemies, 6 * mult, ValueProp.Move, owner, null);
                });

            // E【能 攻 技】所有友方获得1点力量（数值已调整）
            AddChord("E", ChordCategory.Major,
                new[] { CardType.Power, CardType.Attack, CardType.Skill },
                "CUTESAKIKOMOD-ECHORD.title", "CUTESAKIKOMOD-ECHORD.description", "e_chord",
                new[] { 1 },
                async (ctx, owner, mult) =>
                {
                    var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                    foreach (var ally in allies)
                        await PowerCmd.Apply<StrengthPower>(ally, 1 * mult, owner, null);
                });

            // #C【攻 攻】获得1层活力
            AddChord("C#", ChordCategory.Major,
                new[] { CardType.Attack, CardType.Attack },
                "CUTESAKIKOMOD-C#CHORD.title", "CUTESAKIKOMOD-C#CHORD.description", "c_sharp_chord",
                new[] { 1 },
                async (ctx, owner, mult) =>
                {
                    await PowerCmd.Apply<VigorPower>(owner, 1 * mult, owner, null);
                });

            // #D【技 攻】随机造成3点伤害
            AddChord("D#", ChordCategory.Major,
                new[] { CardType.Skill, CardType.Attack },
                "CUTESAKIKOMOD-D#CHORD.title", "CUTESAKIKOMOD-D#CHORD.description", "d_sharp_chord",
                new[] { 3 },
                async (ctx, owner, mult) =>
                {
                    var enemies = owner.CombatState?.HittableEnemies;
                    if (enemies != null && enemies.Any())
                    {
                        var target = owner.CombatState.RunState.Rng.CombatCardSelection.NextItem(enemies);
                        await CreatureCmd.Damage(ctx, target, 3 * mult, ValueProp.Move, owner, null);
                    }
                });

            // ========== 小三和弦 ==========
            // 初始 Am【技 技 攻】所有队友获得3点格挡
            AddChord("Am", ChordCategory.Minor,
                new[] { CardType.Skill, CardType.Skill, CardType.Attack },  // 修改音符序列
                "CUTESAKIKOMOD-AMCHORD.title", "CUTESAKIKOMOD-AMCHORD.description", "am_chord",
                new[] { 4 },
                async (ctx, owner, mult) =>
                {
                    var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                    foreach (var ally in allies)
                        await CreatureCmd.GainBlock(ally, 4 * mult, (ValueProp)0, null);
                });

            // Gm【技 技 技 技】所有友方恢复3点血量
            AddChord("Gm", ChordCategory.Minor,
                new[] { CardType.Skill, CardType.Skill, CardType.Skill, CardType.Skill },
                "CUTESAKIKOMOD-GMCHORD.title", "CUTESAKIKOMOD-GMCHORD.description", "gm_chord",
                new[] { 3 },
                async (ctx, owner, mult) =>
                {
                    var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                    foreach (var ally in allies)
                        await CreatureCmd.Heal(ally, 3 * mult);
                });

            // Em【技 技 攻 技】所有友方本回合获得1点倒映（数值已调整）
            AddChord("Em", ChordCategory.Minor,
                new[] { CardType.Skill, CardType.Skill, CardType.Attack, CardType.Skill },
                "CUTESAKIKOMOD-EMCHORD.title", "CUTESAKIKOMOD-EMCHORD.description", "em_chord",
                new[] { 2 },
                async (ctx, owner, mult) =>
                {
                    var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                    foreach (var ally in allies)
                        await PowerCmd.Apply<ReflectPower>(ally, 2 * mult, owner, null);
                });

            // Dm【技 攻 技】所有友方获得1层再生
            AddChord("Dm", ChordCategory.Minor,
                new[] { CardType.Skill, CardType.Attack, CardType.Skill },
                "CUTESAKIKOMOD-DMCHORD.title", "CUTESAKIKOMOD-DMCHORD.description", "dm_chord",
                new[] { 1 },
                async (ctx, owner, mult) =>
                {
                    var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                    foreach (var ally in allies)
                        await PowerCmd.Apply<RegenPower>(ally, 1 * mult, owner, null);
                });

            // Bm【能 技 技】所有友方获得1点敏捷（数值已调整）
            AddChord("Bm", ChordCategory.Minor,
                new[] { CardType.Power, CardType.Skill, CardType.Skill },
                "CUTESAKIKOMOD-BMCHORD.title", "CUTESAKIKOMOD-BMCHORD.description", "bm_chord",
                new[] { 1 },
                async (ctx, owner, mult) =>
                {
                    var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                    foreach (var ally in allies)
                        await PowerCmd.Apply<DexterityPower>(ally, 1 * mult, owner, null);
                });

            // #Cm【技 技】所有友方获得3点格挡
            AddChord("C#m", ChordCategory.Minor,
                new[] { CardType.Skill, CardType.Skill },
                "CUTESAKIKOMOD-C#MCHORD.title", "CUTESAKIKOMOD-C#MCHORD.description", "c_sharp_m_chord",
                new[] { 2 },
                async (ctx, owner, mult) =>
                {
                    var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                    foreach (var ally in allies)
                        await CreatureCmd.GainBlock(ally, 2 * mult, (ValueProp)0, null);
                });

            // #Dm【攻 攻】所有友方获得1层覆甲(Plating)
            AddChord("D#m", ChordCategory.Minor,
                new[] { CardType.Attack, CardType.Attack },
                "CUTESAKIKOMOD-D#MCHORD.title", "CUTESAKIKOMOD-D#MCHORD.description", "d_sharp_m_chord",
                new[] { 1 },
                async (ctx, owner, mult) =>
                {
                    var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                    foreach (var ally in allies)
                        await PowerCmd.Apply<PlatingPower>(ally, 1 * mult, owner, null);
                });

            // ========== 属七和弦 ==========
            // 初始 G7【攻 技 攻】所有敌人本回合减2力量
            AddChord("G7", ChordCategory.Dominant,
                new[] { CardType.Attack, CardType.Skill, CardType.Attack },
                "CUTESAKIKOMOD-G7CHORD.title", "CUTESAKIKOMOD-G7CHORD.description", "g7_chord",
                new[] { 2 },
                async (ctx, owner, mult) =>
                {
                    var enemies = owner.CombatState?.Enemies;
                    if (enemies != null)
                        foreach (var enemy in enemies)
                            await PowerCmd.Apply<ChordTempStrengthDownPower>(enemy, 2 * mult, owner, null);
                });

            // D7【技 技 攻】所有敌人获得1层虚弱
            AddChord("D7", ChordCategory.Dominant,
                new[] { CardType.Skill, CardType.Skill, CardType.Attack },
                "CUTESAKIKOMOD-D7CHORD.title", "CUTESAKIKOMOD-D7CHORD.description", "d7_chord",
                new[] { 1 },
                async (ctx, owner, mult) =>
                {
                    var enemies = owner.CombatState?.Enemies;
                    if (enemies != null)
                        foreach (var enemy in enemies)
                            await PowerCmd.Apply<WeakPower>(enemy, 1 * mult, owner, null);
                });

            // A7【能 技 技 技】击晕敌人1回合
            AddChord("A7", ChordCategory.Dominant,
                new[] { CardType.Power, CardType.Skill, CardType.Skill, CardType.Skill },
                "CUTESAKIKOMOD-A7CHORD.title", "CUTESAKIKOMOD-A7CHORD.description", "a7_chord",
                new[] { 1 },
                async (ctx, owner, mult) =>
                {
                    var enemies = owner.CombatState?.Enemies;
                    if (enemies != null)
                        foreach (var enemy in enemies)
                            await CreatureCmd.Stun(enemy);
                });

            // E7【技 能 技 能】所有友方获得壁垒(Barricade)
            AddChord("E7", ChordCategory.Dominant,
                new[] { CardType.Skill, CardType.Power, CardType.Skill, CardType.Power },
                "CUTESAKIKOMOD-E7CHORD.title", "CUTESAKIKOMOD-E7CHORD.description", "e7_chord",
                new[] { 1 },
                async (ctx, owner, mult) =>
                {
                    var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                    foreach (var ally in allies)
                        await PowerCmd.Apply<BarricadePower>(ally, 1 * mult, owner, null);
                });

            // #C7【攻 技】所有友方获得1点能量
            AddChord("C#7", ChordCategory.Dominant,
                new[] { CardType.Attack, CardType.Skill },
                "CUTESAKIKOMOD-C#7CHORD.title", "CUTESAKIKOMOD-C#7CHORD.description", "c_sharp_7_chord",
                new[] { 1 },
                async (ctx, owner, mult) =>
                {
                    var player = owner.Player;
                    if (player != null)
                        await PlayerCmd.GainEnergy(1 * mult, player);
                });

            // #D7【技 攻】所有友方抽1张牌
            AddChord("D#7", ChordCategory.Dominant,
                new[] { CardType.Skill, CardType.Attack },
                "CUTESAKIKOMOD-D#7CHORD.title", "CUTESAKIKOMOD-D#7CHORD.description", "d_sharp_7_chord",
                new[] { 1 },
                async (ctx, owner, mult) =>
                {
                    var player = owner.Player;
                    if (player != null)
                        await CardPileCmd.Draw(ctx, 1 * mult, player);
                });
        }

        private static void AddChord(string id, ChordCategory cat, CardType[] seq,
            string titleKey, string descKey, string iconName,
            int[] baseValues,
            Func<PlayerChoiceContext, Creature, int, Task> effect)
        {
            AllChords[id] = new ChordDefinition
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
        }

        /// <summary> 获取指定分类下可学习的和弦ID（排除初始和弦） </summary>
        public static List<string> GetLearnableChordIds(ChordCategory category)
        {
            var exclude = category switch
            {
                ChordCategory.Major => new[] { "C" },
                ChordCategory.Minor => new[] { "Am" },
                ChordCategory.Dominant => new[] { "G7" },
                _ => Array.Empty<string>()
            };
            return AllChords.Values
                .Where(c => c.Category == category && !exclude.Contains(c.Id))
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

        public static bool MatchesChord(ChordDefinition chord, IReadOnlyList<CardType> sequence)
        {
            var pattern = chord.NoteSequence;
            if (sequence.Count < pattern.Length) return false;
            for (int i = 0; i < pattern.Length; i++)
                if (sequence[sequence.Count - pattern.Length + i] != pattern[i])
                    return false;
            return true;
        }
    }
}