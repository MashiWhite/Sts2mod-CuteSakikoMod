using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
        _temporaryChordIds.Add(id);
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

                await CreatureCmd.GainBlock(owner, 3 * mult, 0, null);
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
                    await PowerCmd.Apply<VigorPower>(ctx, ally, 3 * mult, owner, null);
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
                    await PowerCmd.Apply<VulnerablePower>(ctx, ally, 1 * mult, owner, null);
                    await PowerCmd.Apply<StrengthPower>(ctx, ally, 2 * mult, owner, null);
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
                    await PowerCmd.Apply<StrengthPower>(ctx, ally, 1 * mult, owner, null);
            });

        // #C【攻 攻】获得1层活力
        AddChord("C#", ChordCategory.Major,
            new[] { CardType.Attack, CardType.Attack },
            "CUTESAKIKOMOD-C#CHORD.title", "CUTESAKIKOMOD-C#CHORD.description", "c_sharp_chord",
            new[] { 1 },
            async (ctx, owner, mult) => { await PowerCmd.Apply<VigorPower>(ctx, owner, 1 * mult, owner, null); });

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
            new[] { CardType.Skill, CardType.Skill, CardType.Attack }, // 修改音符序列
            "CUTESAKIKOMOD-AMCHORD.title", "CUTESAKIKOMOD-AMCHORD.description", "am_chord",
            new[] { 4 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                    await CreatureCmd.GainBlock(ally, 4 * mult, 0, null);
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

        // Em【技 技 攻 技】所有友方本回合获得1点倒映
        AddChord("Em", ChordCategory.Minor,
            new[] { CardType.Skill, CardType.Skill, CardType.Attack, CardType.Skill },
            "CUTESAKIKOMOD-EMCHORD.title", "CUTESAKIKOMOD-EMCHORD.description", "em_chord",
            new[] { 1 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                    await PowerCmd.Apply<ReflectPower>(ctx, ally, 1 * mult, owner, null);
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
                    await PowerCmd.Apply<RegenPower>(ctx, ally, 1 * mult, owner, null);
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
                    await PowerCmd.Apply<DexterityPower>(ctx, ally, 1 * mult, owner, null);
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
                    await CreatureCmd.GainBlock(ally, 2 * mult, 0, null);
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
                    await PowerCmd.Apply<PlatingPower>(ctx, ally, 1 * mult, owner, null);
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
                        await PowerCmd.Apply<ChordTempStrengthDownPower>(ctx, enemy, 2 * mult, owner, null);
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
                        await PowerCmd.Apply<WeakPower>(ctx, enemy, 1 * mult, owner, null);
            });

        // A7【能 技 能 技】击晕敌人1回合
        AddChord("A7", ChordCategory.Dominant,
            new[] { CardType.Power, CardType.Skill, CardType.Power, CardType.Skill },
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
                    await PowerCmd.Apply<BarricadePower>(ctx, ally, 1 * mult, owner, null);
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

        //爱音C和弦
        AddTemporaryChord("AnonCChord", ChordCategory.Anon,
            new[] { CardType.Skill, CardType.Skill, CardType.Skill },
            "CUTESAKIKOMOD-ANONCCHORD.title", "CUTESAKIKOMOD-ANONCCHORD.description", "anon_c_chord",
            new[] { 1 }, // 基础升级张数
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                var upgradeCount = 1 * mult; // 普通为1，先古为2
                var allUpgradable = new List<CardModel>();

                // 收集所有友方手牌中可升级的牌
                foreach (var player in combat.Players)
                {
                    var hand = player.PlayerCombatState?.Hand;
                    if (hand == null) continue;
                    allUpgradable.AddRange(hand.Cards.Where(c => c.IsUpgradable));
                }

                // 随机选择指定数量的卡牌升级
                var rng = combat.RunState.Rng.CombatCardSelection;
                var selected = allUpgradable.OrderBy(x => rng.NextInt()).Take(upgradeCount).ToList();

                foreach (var card in selected)
                {
                    CardCmd.Upgrade(card);
                    await Cmd.CustomScaledWait(0.15f, 0.2f);
                }
            });

        // 在 RegisterChords 方法中添加，建议放在其他爱音临时和弦附近
        AddTemporaryChord("AnonDChord", ChordCategory.Anon,
            new[] { CardType.Skill, CardType.Attack, CardType.Attack, CardType.Attack },
            "CUTESAKIKOMOD-ANONDCHORD.title", "CUTESAKIKOMOD-ANONDCHORD.description", "anon_d_chord",
            new int[0],
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                // 获取所有友方玩家（包括自己）
                var allies = combat.Players.ToList();
                foreach (var player in allies)
                {
                    // 为每个友方生成一瓶随机药水并加入其药水栏
                    var randomPotion = PotionFactory.CreateRandomPotionInCombat(
                        player,
                        player.RunState.Rng.CombatPotionGeneration
                    ).ToMutable();
                    await PotionCmd.TryToProcure(randomPotion, player);
                }
            });

        // 在 ChordManager.RegisterChords() 中添加
        AddTemporaryChord("AnonEChord", ChordCategory.Anon,
            new[] { CardType.Skill, CardType.Attack, CardType.Skill, CardType.Skill },
            "CUTESAKIKOMOD-ANONECHORD.title", "CUTESAKIKOMOD-ANONECHORD.description", "anon_e_chord",
            new[] { 1, 4 }, // 残影层数, 格挡
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

        // 爱音F和弦【攻 攻 攻 攻】50%对全体敌人造成12点伤害，50%使所有友方获得1层虚弱
        AddTemporaryChord("AnonFChord", ChordCategory.Anon,
            new[] { CardType.Attack, CardType.Attack, CardType.Attack, CardType.Attack },
            "CUTESAKIKOMOD-ANONFCHORD.title", "CUTESAKIKOMOD-ANONFCHORD.description", "anon_f_chord",
            new[] { 12, 1 }, // BaseValues: [伤害值, 虚弱层数]
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                // 50% 概率判定（使用战斗专用随机）
                var dealDamage = combat.RunState.Rng.CombatCardSelection.NextDouble() < 0.75;

                if (dealDamage)
                {
                    // 对全体敌人造成 12 * mult 点伤害
                    var enemies = combat.Enemies;
                    if (enemies != null && enemies.Any())
                        await CreatureCmd.Damage(ctx, enemies, 12 * mult, ValueProp.Move, owner, null);
                }
                else
                {
                    // 对全体友方施加 1 * mult 层虚弱
                    var allies = combat.Players.Select(p => p.Creature) ?? new[] { owner };
                    foreach (var ally in allies)
                        await PowerCmd.Apply<WeakPower>(ctx, ally, 1 * mult, owner, null);
                }
            });

        // 爱音G和弦【技 技 攻】所有友方抽1牌，获1能量
        AddTemporaryChord("AnonGChord", ChordCategory.Anon,
            new[] { CardType.Skill, CardType.Skill, CardType.Attack },
            "CUTESAKIKOMOD-ANONGCHORD.title", "CUTESAKIKOMOD-ANONGCHORD.description", "anon_g_chord",
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

        //灰爱音和弦【特 攻 攻】 减少2生命，抽1获得1能量
        AddTemporaryChord("GreyAnonChord", ChordCategory.Anon,
            new[] { CardType.Status, CardType.Attack, CardType.Attack },
            "CUTESAKIKOMOD-GREYANONCHORD.title", "CUTESAKIKOMOD-GREYANONCHORD.description", "grey_anon_chord",
            new[] { 2, 1, 1 }, // 生命减少、抽牌、能量
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                // 失去 1*mult 点生命值（不可格挡，不受力量影响）
                await CreatureCmd.Damage(ctx, owner, 1 * mult,
                    ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                    owner, null);

                // 抽牌
                var player = owner.Player;
                if (player != null)
                    await CardPileCmd.Draw(ctx, 1 * mult, player);

                // 获得能量
                if (player != null)
                    await PlayerCmd.GainEnergy(1 * mult, player);
            });

        //碧天伴走
        AddTemporaryChord("HekitenbansouChord", ChordCategory.Anon,
            new[] { CardType.Attack, CardType.Skill, CardType.Attack, CardType.Skill },
            "CUTESAKIKOMOD-HEKITENBANSOUCHORD.title", "CUTESAKIKOMOD-HEKITENBANSOUCHORD.description",
            "hekitenbansou_chord",
            new[] { 1 }, // 基础转化层数，会随倍率变化
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                var allies = combat.Players.Select(p => p.Creature).ToList();

                foreach (var ally in allies)
                {
                    // 脆弱 → 敏捷（固定1层的倍数）
                    var frail = ally.Powers.OfType<FrailPower>().FirstOrDefault();
                    if (frail != null)
                    {
                        frail.RemoveInternal();
                        await PowerCmd.Apply<DexterityPower>(ctx, ally, 1 * mult, owner, null);
                    }

                    // 虚弱 → 力量（固定1层的倍数）
                    var weak = ally.Powers.OfType<WeakPower>().FirstOrDefault();
                    if (weak != null)
                    {
                        weak.RemoveInternal();
                        await PowerCmd.Apply<StrengthPower>(ctx, ally, 1 * mult, owner, null);
                    }
                }
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

    //获得临时和弦
    public static List<string> GetTemporaryChordIds(ChordCategory? category = null)
    {
        var query = _temporaryChordIds.Where(id => AllChords[id].IsTemporaryOnly);
        if (category.HasValue)
            query = query.Where(id => AllChords[id].Category == category.Value);
        return query.ToList();
    }

    /// <summary> 获取指定分类下可学习的和弦ID（排除初始和弦） </summary>
    public static List<string> GetLearnableChordIds(ChordCategory category)
    {
        // 爱音分类不参与学习
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

    public static bool MatchesChord(ChordDefinition chord, IReadOnlyList<CardType> sequence)
    {
        var pattern = chord.NoteSequence;
        if (sequence.Count < pattern.Length) return false;
        for (var i = 0; i < pattern.Length; i++)
        {
            var expected = pattern[i];
            var actual = sequence[sequence.Count - pattern.Length + i];

            // 如果和弦定义中的类型是 Status，代表“特”（通配非攻/技/能）
            if (expected == CardType.Status)
            {
                // 匹配除 Attack、Skill、Power 之外的所有类型
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