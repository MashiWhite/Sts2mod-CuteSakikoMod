using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;
using MegaCrit.Sts2.Core.Rooms;
using Timer = Godot.Timer;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;

[RegisterMonster]
public class GreyAnon : ModMonsterTemplate
{
    private bool _isPhaseTwo;
    private MoveState _performState;
    private Timer? _greyTextTimer;
    private int _monologueIndex = 0;
    private List<string> _monologueKeys = new();
    private Vector2 _lastGreyTextPosition = new Vector2(-1000, -1000); // 记录上一次位置，避免重叠

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 950, 850);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 1030, 930);

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/grey_anon_boss.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    public NCreatureVisuals? CreateLiveVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            "res://CuteSakikoMod/scenes/monster/grey_anon_boss_live.tscn");
    }

    public NCreatureVisuals? CreateOriginalVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<HeartWallPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        await PowerCmd.Apply<BecomeAshesPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Creature) return;
        if (!_isPhaseTwo && Creature.CurrentHp <= MaxInitialHp * 0.85)
        {
            _isPhaseTwo = true;
            await PowerCmd.Apply<BecomeAshesPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
            SetMoveImmediate(_performState, true);
        }
    }

    // 初始化台词列表（按数字顺序）
    private void InitializeMonologueKeys()
    {
        if (_monologueKeys.Count > 0) return;

        const string prefix = "CUTE_SAKIKO_MOD_MONSTER_GREY_ANON.monologue";
        var table = LocManager.Instance.GetTable("monsters");
        var allLocStrings = table.GetLocStringsWithPrefix(prefix);
        _monologueKeys = allLocStrings
            .Select(loc => loc.LocEntryKey)
            .OrderBy(key =>
            {
                var numStr = key.Substring(prefix.Length + 1);
                return int.TryParse(numStr, out var num) ? num : 0;
            })
            .ToList();

        if (_monologueKeys.Count == 0)
            _monologueKeys = new List<string>();
    }

    // 启动定时器
    private void StartGreyTextTimer()
    {
        StopGreyTextTimer();

        var room = NCombatRoom.Instance;
        if (room == null) return;

        _greyTextTimer = new Timer
        {
            WaitTime = 4.0f,          // 4 秒间隔
            OneShot = false,
            Autostart = true
        };
        _greyTextTimer.Timeout += SpawnMonologueSequentially;
        room.AddChild(_greyTextTimer);
    }

    // 停止定时器
    private void StopGreyTextTimer()
    {
        if (_greyTextTimer != null)
        {
            if (GodotObject.IsInstanceValid(_greyTextTimer))
            {
                _greyTextTimer.Stop();
                _greyTextTimer.Timeout -= SpawnMonologueSequentially;
                _greyTextTimer.QueueFree();
            }
            _greyTextTimer = null;
        }
    }

    // 顺序播放台词
    private void SpawnMonologueSequentially()
    {
        if (Creature == null || Creature.IsDead || NCombatRoom.Instance == null)
        {
            StopGreyTextTimer();
            return;
        }

        if (_monologueKeys.Count == 0) return;

        var key = _monologueKeys[_monologueIndex];
        var locString = LocManager.Instance.GetTable("monsters").GetLocString(key);
        if (locString != null && !locString.IsEmpty)
        {
            string text = locString.GetFormattedText();
            GreyTextManager.Spawn(text, GetRandomGreyTextPosition());
        }

        _monologueIndex = (_monologueIndex + 1) % _monologueKeys.Count;
    }

    // 生成不重叠的随机位置
    private Vector2 GetRandomGreyTextPosition()
    {
        const float minDistance = 300f;
        Vector2 newPos;
        int attempts = 0;
        do
        {
            newPos = new Vector2(
                (float)GD.RandRange(450, 1280),
                (float)GD.RandRange(300, 750)
            );
            attempts++;
        } while (attempts < 20 && _lastGreyTextPosition.DistanceTo(newPos) < minDistance);

        _lastGreyTextPosition = newPos;
        return newPos;
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        StopGreyTextTimer();
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        StopGreyTextTimer();
        await base.AfterCombatEnd(room);
    }

    // 公开的启动方法，供 AiHeartPower 调用
    public void StartGreyText()
    {
        InitializeMonologueKeys();
        StartGreyTextTimer();
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var monologue = new MoveState("MONOLOGUE", MonologueMove,
            new SingleAttackIntent(10));
        var heavy1 = new MoveState("HEAVY_ATTACK_1", HeavyAttack1Move,
            new SingleAttackIntent(15), new DefendIntent());
        var devote1 = new MoveState("DEVOTE_1", Devote1Move,
            new DebuffIntent(), new HealIntent());
        var buff1 = new MoveState("BUFF_1", Buff1Move,
            new SingleAttackIntent(8), new DebuffIntent());

        monologue.FollowUpState = heavy1;
        heavy1.FollowUpState = devote1;
        devote1.FollowUpState = buff1;
        buff1.FollowUpState = heavy1;

        _performState = new MoveState("PERFORM", PerformMove,
            new DebuffIntent(), new DefendIntent());
        var heavy2 = new MoveState("HEAVY_ATTACK_2", HeavyAttack2Move,
            new SingleAttackIntent(20), new DefendIntent());
        var devote2 = new MoveState("DEVOTE_2", Devote2Move,
            new DebuffIntent(), new HealIntent());

        _performState.FollowUpState = heavy2;
        heavy2.FollowUpState = devote2;
        devote2.FollowUpState = _performState;

        var states = new List<MonsterState>
        {
            monologue, heavy1, devote1, buff1,
            _performState, heavy2, devote2
        };

        return new MonsterMoveStateMachine(states, monologue);
    }

    // ========== 招式实现 ==========

    private async Task MonologueMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(10).FromMonster(this).Execute(null);
    }

    private async Task HeavyAttack1Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(15).FromMonster(this).Execute(null);
        await CreatureCmd.GainBlock(Creature, 30, ValueProp.Move, null);
    }

    private async Task Devote1Move(IReadOnlyList<Creature> targets)
    {
        var combatState = Creature.CombatState as CombatState;
        foreach (var player in Creature.CombatState.Players)
        {
            var wasDead = player.Creature.IsDead;
            await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(),
                player.Creature, -1, Creature, null, true);
            await CreatureCmd.Heal(player.Creature, 10);
            if (wasDead && player.Creature.IsAlive && combatState != null)
            {
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
                await CreatureCmd.TriggerAnim(player.Creature, "idle_loop", 0f);
            }
        }
    }

    private async Task Buff1Move(IReadOnlyList<Creature> targets)
    {
        foreach (var player in Creature.CombatState.Players)
            await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(),
                player.Creature, 3, Creature, null);
        await DamageCmd.Attack(8).FromMonster(this).Execute(null);
    }

    private async Task PerformMove(IReadOnlyList<Creature> targets)
    {
        foreach (var player in Creature.CombatState.Players)
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(),
                player.Creature, -2, Creature, null, true);
        await CreatureCmd.GainBlock(Creature, 30, ValueProp.Move, null);
    }

    private async Task HeavyAttack2Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(20).FromMonster(this).Execute(null);
        await CreatureCmd.GainBlock(Creature, 45, ValueProp.Move, null);
    }

    private async Task Devote2Move(IReadOnlyList<Creature> targets)
    {
        var combatState = Creature.CombatState as CombatState;
        foreach (var player in Creature.CombatState.Players)
        {
            var wasDead = player.Creature.IsDead;
            await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(),
                player.Creature, 3, Creature, null);
            await CreatureCmd.Heal(player.Creature, 5);
            if (wasDead && player.Creature.IsAlive && combatState != null)
            {
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
                await CreatureCmd.TriggerAnim(player.Creature, "idle_loop", 0f);
            }
        }
    }
}