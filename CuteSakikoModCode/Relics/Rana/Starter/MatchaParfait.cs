using CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Status;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;

[RegisterCharacterStarterRelic(typeof(CuteRana), Order = 0)]
[RegisterTouchOfOrobasRefinement(typeof(BigMatchaParfait))]
public class MatchaParfait : CuteRanaRelic, IModRightClickableRelic,
    IRelicExtraIconAmountLabelSpecsProvider, IRelicExtraIconAmountLabelsChangeSource
{
    // 私有字段，序列化器会通过属性访问
    private int _charges;
    private int _currentTurnCount;
    private int _drawAmount = 1;
    private int _energyGain = 1;

    // 公共属性，标记 [SavedProperty] 确保序列化
    [SavedProperty]
    public int Charges
    {
        get => _charges;
        set
        {
            if (_charges == value) return;
            _charges = value;
            InvokeDisplayAmountChanged();
        }
    }

    [SavedProperty]
    public int CurrentTurnCount
    {
        get => _currentTurnCount;
        private set
        {
            if (_currentTurnCount == value) return;
            _currentTurnCount = value;
            RelicExtraIconAmountLabelsInvalidated?.Invoke();
            InvokeDisplayAmountChanged();
        }
    }
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<BrainFreeze>();
        }
    }

    public int DrawAmount
    {
        get => _drawAmount;
        set
        {
            _drawAmount = value;
            if (DynamicVars.TryGetValue("Cards", out var dv)) dv.BaseValue = value;
        }
    }

    public int EnergyGain
    {
        get => _energyGain;
        set
        {
            _energyGain = value;
            if (DynamicVars.TryGetValue("Energy", out var dv)) dv.BaseValue = value;
        }
    }

    // 虚方法，子类可覆盖以修改初始杯数
    protected virtual int GetInitialCharges() => 6;

    public MatchaParfait()
    {
        // 初始化杯数（仅用于新获得的遗物，加载存档时会被覆盖）
        Charges = GetInitialCharges();
    }

    public event Action? RelicExtraIconAmountLabelsInvalidated;

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => true;
    public override int DisplayAmount => Charges;

    public static event Action<Player, int, PlayerChoiceContext?>? OnChargesRemoved;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(DrawAmount), new EnergyVar(EnergyGain) };

    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        if (Owner?.Creature?.CombatState == null) return false;
        if (Owner.Creature.CombatState.CurrentSide != CombatSide.Player) return false;
        return Charges > 0;
    }

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        var player = context.Player;
        try
        {
            Entry.Logger.Info("[芭菲] 效果开始");
            await CardPileCmd.Draw(context.PlayerChoiceContext, DrawAmount, player);
            await PlayerCmd.GainEnergy(EnergyGain, player);
            RemoveCharges(this, 1, context.PlayerChoiceContext);
            Entry.Logger.Info("[芭菲] 效果完成");
        }
        catch (Exception ex)
        {
            Entry.Logger.Error($"[芭菲] 效果异常: {ex}");
        }
    }

    public IReadOnlyList<ExtraIconAmountLabelSpec> GetRelicExtraIconAmountLabelSpecs()
    {
        return new[]
        {
            ExtraIconAmountLabelSpec.Plain(ExtraIconAmountLabelCorner.TopRight, CurrentTurnCount.ToString())
        };
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Player && Owner != null)
        {
            CurrentTurnCount = 0;
        }
        RelicExtraIconAmountLabelsInvalidated?.Invoke();
        InvokeDisplayAmountChanged();
        await Task.CompletedTask;
    }

    private async Task OnParfaitConsumedInstanceAsync(int amount, PlayerChoiceContext? choiceContext)
    {
        for (int i = 0; i < amount; i++)
        {
            CurrentTurnCount++;
            Entry.Logger.Info($"[芭菲] 当前回合计数={CurrentTurnCount}");

            if (CurrentTurnCount >= 4)
            {
                if (choiceContext != null)
                {
                    var combatState = Owner.Creature.CombatState;
                    if (combatState != null)
                    {
                        var brainFreeze = combatState.CreateCard<BrainFreeze>(Owner);
                        var result = await CardPileCmd.AddGeneratedCardToCombat(brainFreeze, PileType.Draw, Owner);
                        CardCmd.PreviewCardPileAdd(result);
                        Entry.Logger.Info("[芭菲] 添加吃到头疼并预览");
                    }
                    else
                    {
                        Entry.Logger.Warn("[芭菲] combatState 为 null，无法添加 BrainFreeze");
                    }
                }
                else
                {
                    Entry.Logger.Warn("[芭菲] choiceContext 为 null，无法添加 BrainFreeze");
                }
                CurrentTurnCount = 0;
                break;
            }
        }
    }
    
    // 在 MatchaParfait 类中添加（与 RemoveCharges 放在一起）
    public static void AddCharges(MatchaParfait relic, int amount)
    {
        if (relic == null) return;
        relic.Charges += amount;
    }

    public static void RemoveCharges(MatchaParfait relic, int amount, PlayerChoiceContext? choiceContext = null)
    {
        if (relic == null) return;

        bool hasTreat = relic.Owner.Creature.HasPower<ParfaitTreatPower>();
        if (hasTreat)
        {
            Entry.Logger.Info($"[芭菲] 有人请客，不扣除杯数，但计数{amount}次");
            OnChargesRemoved?.Invoke(relic.Owner, amount, choiceContext);
            _ = relic.OnParfaitConsumedInstanceAsync(amount, choiceContext);
            return;
        }

        int old = relic.Charges;
        relic.Charges = Math.Max(0, relic.Charges - amount);
        int removed = old - relic.Charges;
        if (removed > 0)
        {
            OnChargesRemoved?.Invoke(relic.Owner, removed, choiceContext);
            _ = relic.OnParfaitConsumedInstanceAsync(amount, choiceContext);
        }
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is RestSiteRoom) Charges += 5;
        return Task.CompletedTask;
    }
}