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
    [SavedProperty]
    private int _charges = 6;

    [SavedProperty]
    private int _currentTurnCount;

    private int _drawAmount = 1;
    private int _energyGain = 1;

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
    
    public int DrawAmount { get => _drawAmount; set { _drawAmount = value; if (DynamicVars.TryGetValue("Cards", out var dv)) dv.BaseValue = value; } }
    public int EnergyGain { get => _energyGain; set { _energyGain = value; if (DynamicVars.TryGetValue("Energy", out var dv)) dv.BaseValue = value; } }

    public int Charges { get => _charges; set { if (_charges == value) return; _charges = value; InvokeDisplayAmountChanged(); } }

    public event Action? RelicExtraIconAmountLabelsInvalidated;

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => true;
    public override int DisplayAmount => Charges;

    // ★ 恢复静态事件，供其他模组（如 WantBothPower）监听
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
        // 读档后强制刷新角标
        RelicExtraIconAmountLabelsInvalidated?.Invoke();
        InvokeDisplayAmountChanged();
        await Task.CompletedTask;
    }

    // ★ 实例方法：处理自身计数和添加头疼卡牌
    private void OnParfaitConsumedInstance(int amount, PlayerChoiceContext? choiceContext)
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
                        _ = CardPileCmd.AddGeneratedCardToCombat(brainFreeze, PileType.Draw, Owner);
                        Entry.Logger.Info("[芭菲] 添加吃到头疼");
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

    // ★ 静态方法：供卡牌调用，同时触发静态事件和实例方法
    public static void RemoveCharges(MatchaParfait relic, int amount, PlayerChoiceContext? choiceContext = null)
    {
        if (relic == null) return;

        bool hasTreat = relic.Owner.Creature.HasPower<ParfaitTreatPower>();
        if (hasTreat)
        {
            Entry.Logger.Info($"[芭菲] 有人请客，不扣除杯数，但计数{amount}次");
            // 触发静态事件（供其他监听者，如 WantBothPower）
            OnChargesRemoved?.Invoke(relic.Owner, amount, choiceContext);
            // 同时更新自身计数
            relic.OnParfaitConsumedInstance(amount, choiceContext);
            return;
        }

        int old = relic.Charges;
        relic.Charges = Math.Max(0, relic.Charges - amount);
        int removed = old - relic.Charges;
        if (removed > 0)
        {
            OnChargesRemoved?.Invoke(relic.Owner, removed, choiceContext);
            relic.OnParfaitConsumedInstance(removed, choiceContext);
        }
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is RestSiteRoom) Charges += 5;
        return Task.CompletedTask;
    }

    public static void AddCharges(MatchaParfait relic, int amount) => relic.Charges += amount;
    public static void SetDrawAmount(MatchaParfait relic, int amount) => relic.DrawAmount = amount;
    public static void SetEnergyGain(MatchaParfait relic, int amount) => relic.EnergyGain = amount;
}