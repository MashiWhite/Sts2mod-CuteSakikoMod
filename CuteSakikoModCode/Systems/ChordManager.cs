using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Combat;
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
    public static List<ChordDefinition> AllChordsList { get; } = new(); // 新增

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
        // 初始 C【攻 攻 技】随机造成3点伤害并获得3点格挡
        // C 和弦
        AddChord("C", ChordCategory.Major,
            new[] { CardType.Attack, CardType.Attack, CardType.Skill },
            "CUTESAKIKOMOD-CCHORD.title", "CUTESAKIKOMOD-CCHORD.description", "c_chord",
            new[] { 3, 3 },
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                // 1. 获取已排序的敌人列表，保证两端顺序一致
                var enemies = combat.HittableEnemies
                    .OrderBy(e => e.Monster?.Id.Entry)
                    .ToList();

                if (enemies.Any())
                {
                    // 2. 用同步随机数随机选一个
                    var target = combat.RunState.Rng.CombatCardSelection.NextItem(enemies);
                    await CreatureCmd.Damage(ctx, target, 3 * mult, ValueProp.Move, owner, null);
                }

                // 3. 获得格挡（无随机）
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

        // D【技 攻 攻 攻】所有友方获得1层脆弱和2点力量
        AddChord("D", ChordCategory.Major,
            new[] { CardType.Skill, CardType.Attack, CardType.Attack, CardType.Attack },
            "CUTESAKIKOMOD-DCHORD.title", "CUTESAKIKOMOD-DCHORD.description", "d_chord",
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
                var combat = owner.CombatState;
                if (combat == null) return;

                var enemies = combat.HittableEnemies
                    .OrderBy(e => e.Monster?.Id.Entry)
                    .ToList();

                if (enemies.Any())
                {
                    var target = combat.RunState.Rng.CombatCardSelection.NextItem(enemies);
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

        // Gm【技 技 技 技】所有友方恢复3点血量，复活时恢复抽牌堆并播放待机动画
        AddChord("Gm", ChordCategory.Minor,
            new[] { CardType.Skill, CardType.Skill, CardType.Skill, CardType.Skill },
            "CUTESAKIKOMOD-GMCHORD.title", "CUTESAKIKOMOD-GMCHORD.description", "gm_chord",
            new[] { 3 },
            async (ctx, owner, mult) =>
            {
                var allies = owner.CombatState?.Players.Select(p => p.Creature) ?? new[] { owner };
                var combatState = owner.CombatState as CombatState;
                foreach (var ally in allies)
                {
                    var wasDead = ally.IsDead;
                    await CreatureCmd.Heal(ally, 3 * mult);

                    // 复活后恢复抽牌堆
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

                        // ★ 复活后自动播放待机动画（避免卡在死亡动画）
                        await CreatureCmd.TriggerAnim(ally, "idle_loop", 0f);
                    }
                }
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

        // #Em【技 技 攻 技】所有友方获得1层虚弱，获得8点格挡
        AddChord("E#m", ChordCategory.Minor,
            new[] { CardType.Skill, CardType.Skill, CardType.Attack, CardType.Skill },
            "CUTESAKIKOMOD-E#MCHORD.title", "CUTESAKIKOMOD-E#MCHORD.description", "e_sharp_m_chord",
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
                {
                    foreach (var enemy in enemies.Where(e => e.IsAlive))
                    {
                        await CreatureCmd.Stun(enemy);
                    }
                }
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
                var combat = owner.CombatState;
                if (combat != null)
                    foreach (var player in combat.Players)
                        await PlayerCmd.GainEnergy(1 * mult, player);
            });

        // #D7【技 攻】所有友方抽1张牌
        AddChord("D#7", ChordCategory.Dominant,
            new[] { CardType.Skill, CardType.Attack },
            "CUTESAKIKOMOD-D#7CHORD.title", "CUTESAKIKOMOD-D#7CHORD.description", "d_sharp_7_chord",
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
            "CUTESAKIKOMOD-ANONCCHORD.title", "CUTESAKIKOMOD-ANONCCHORD.description", "anon_c_chord",
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

                    // 每人升级 mult 张（不够就全升）
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

        // AnonDChord,Osusume,喝到晕碳
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

        // 爱音F和弦【攻 攻 攻 攻】对全体敌人造成20点伤害，使所有友方获得1层虚弱
        AddTemporaryChord("AnonFChord", ChordCategory.Anon,
            new[] { CardType.Attack, CardType.Attack, CardType.Attack, CardType.Attack },
            "CUTESAKIKOMOD-ANONFCHORD.title", "CUTESAKIKOMOD-ANONFCHORD.description", "anon_f_chord",
            new[] { 20, 1 }, // BaseValues: [伤害值, 虚弱层数]
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;
                var enemies = combat.Enemies;
                if (enemies != null && enemies.Any())
                    await CreatureCmd.Damage(ctx, enemies, 20 * mult, ValueProp.Move, owner, null);
                // 对全体友方施加 1 * mult 层虚弱
                var allies = combat.Players.Select(p => p.Creature) ?? new[] { owner };
                foreach (var ally in allies)
                {
                    var weak2 = await PowerCmd.Apply<WeakPower>(ctx, ally, 1 * mult, owner, null);
                    if (weak2 != null) weak2.SkipNextDurationTick = false;
                }
            });

        // 爱音G和弦【技 技 攻 攻】所有友方抽1牌，获1能量
        AddTemporaryChord("AnonGChord", ChordCategory.Anon,
            new[] { CardType.Skill, CardType.Skill, CardType.Attack, CardType.Attack },
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

        //灰爱音和弦【特 攻 攻】 对自己造成2点伤害，所有友方抽1获得1能量
        AddTemporaryChord("GreyAnonChord", ChordCategory.Anon,
            new[] { CardType.Status, CardType.Attack, CardType.Attack },
            "CUTESAKIKOMOD-GREYANONCHORD.title", "CUTESAKIKOMOD-GREYANONCHORD.description", "grey_anon_chord",
            new[] { 2, 1, 1 }, // 伤害值, 抽牌数, 能量数
            async (ctx, owner, mult) =>
            {
                var combat = owner.CombatState;
                if (combat == null) return;

                // 对自己造成伤害（不可格挡，不受力量影响）
                await CreatureCmd.Damage(ctx, owner, 2 * mult,
                    ValueProp.Unblockable | ValueProp.Unpowered,
                    owner, null);

                // 所有友方（包括自己）抽牌并获得能量
                foreach (var player in combat.Players)
                {
                    if (player == null) continue;
                    await CardPileCmd.Draw(ctx, 1 * mult, player);
                    await PlayerCmd.GainEnergy(1 * mult, player);
                }
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

    public static bool MatchesChord(IReadOnlyList<CardType> pattern, IReadOnlyList<CardType> sequence)
    {
        if (sequence.Count < pattern.Count) return false;
        for (var i = 0; i < pattern.Count; i++)
        {
            var expected = pattern[i];
            var actual = sequence[sequence.Count - pattern.Count + i];

            if (expected == CardType.Status) // 通配符：匹配除攻/技/能之外的所有类型
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