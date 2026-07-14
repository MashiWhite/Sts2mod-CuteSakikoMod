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
using MegaCrit.Sts2.Core.Helpers;
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
    private int _totalConsumedThisCombat;
    private int _charges;
    private int _currentTurnCount;
    private int _drawAmount = 1;
    private int _energyGain = 1;

    [SavedProperty]
    public int TotalConsumedThisCombat
    {
        get => _totalConsumedThisCombat;
        set
        {
            if (_totalConsumedThisCombat == value) return;
            _totalConsumedThisCombat = value;
            InvokeDisplayAmountChanged();
        }
    }

    [SavedProperty]
    public int CurrentTurnCount
    {
        get => _currentTurnCount;
        set
        {
            if (_currentTurnCount == value) return;
            _currentTurnCount = value;
            RelicExtraIconAmountLabelsInvalidated?.Invoke();
            InvokeDisplayAmountChanged();
        }
    }

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

    protected virtual int GetInitialCharges() => 6;

    public MatchaParfait()
    {
        Charges = GetInitialCharges();
    }

    public event Action? RelicExtraIconAmountLabelsInvalidated;
    public event Action<Player, int, PlayerChoiceContext?>? ChargesRemoved;

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => true;
    public override int DisplayAmount => Charges;

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
            await RemoveCharges(this, 1, context.PlayerChoiceContext, ignoreTreat: true);
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
            if (combatState.RoundNumber == 1)
                TotalConsumedThisCombat = 0;
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
                        var result = await CardPileCmd.AddGeneratedCardToCombat(brainFreeze, PileType.Draw, Owner, CardPilePosition.Random);
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

    // ===== 核心修改：改为 async Task，并 await 两者皆要 =====
    public static async Task SimulateParfaitEaten(Player player, int amount, PlayerChoiceContext? choiceContext)
    {
        var relic = player.Relics.OfType<MatchaParfait>().FirstOrDefault();
        if (relic != null)
        {
            relic.TotalConsumedThisCombat += amount;
            relic.ChargesRemoved?.Invoke(player, amount, choiceContext);
            _ = relic.OnParfaitConsumedInstanceAsync(amount, choiceContext);
            if (player.Creature.HasPower<WantBothPower>())
            {
                await ApplyWantBothEffect(player, amount, choiceContext);
            }
        }
        else
        {
            if (player.Creature.HasPower<WantBothPower>())
            {
                await ApplyWantBothEffect(player, amount, choiceContext);
            }
        }
    }

    public static void AddCharges(MatchaParfait relic, int amount)
    {
        if (relic == null) return;
        relic.Charges += amount;
    }

    // ===== 修复：有人请客时也触发两者皆要 =====
    public static async Task RemoveCharges(MatchaParfait relic, int amount, PlayerChoiceContext? choiceContext = null, bool ignoreTreat = false)
    {
        if (relic == null) return;

        bool hasTreat = relic.Owner.Creature.HasPower<ParfaitTreatPower>();

        if (hasTreat && !ignoreTreat)
        {
            Entry.Logger.Info($"[芭菲] 有人请客，不扣除杯数，但计数{amount}次");
            relic.TotalConsumedThisCombat += amount;
            relic.ChargesRemoved?.Invoke(relic.Owner, amount, choiceContext);
            _ = relic.OnParfaitConsumedInstanceAsync(amount, choiceContext);

            // 即使不扣杯数，也要触发“两者皆要”
            if (relic.Owner.Creature.HasPower<WantBothPower>())
            {
                await ApplyWantBothEffect(relic.Owner, amount, choiceContext);
            }
            return;
        }

        int old = relic.Charges;
        relic.Charges = Math.Max(0, relic.Charges - amount);
        int removed = old - relic.Charges;
        if (removed > 0)
        {
            relic.TotalConsumedThisCombat += removed;
            relic.ChargesRemoved?.Invoke(relic.Owner, removed, choiceContext);
            _ = relic.OnParfaitConsumedInstanceAsync(removed, choiceContext);

            if (relic.Owner.Creature.HasPower<WantBothPower>())
            {
                await ApplyWantBothEffect(relic.Owner, removed, choiceContext);
            }
        }
    }

    private static async Task ApplyWantBothEffect(Player player, int amount, PlayerChoiceContext? choiceContext)
    {
        for (int i = 0; i < amount; i++)
        {
            await PlayerCmd.GainEnergy(1, player);
            if (choiceContext != null)
                await CardPileCmd.Draw(choiceContext, 1, player);
        }
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is RestSiteRoom) Charges += 5;
        return Task.CompletedTask;
    }
}