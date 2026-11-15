using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class HeartWallPower : CuteSakikoModPower
{
    // ★ 按 owner 生物实例分别记录：每只怪自己的状态，绝不跨怪物共享。
    //   用 ConditionalWeakTable 挂在 Creature 上，避免生物释放后泄漏。
    private static readonly ConditionalWeakTable<Creature, WallState> States = new();

    /// <summary>层数 = 本回合内同一张卡造成多少次伤害后免疫；默认 1（灰爱音本体），恐惧 dominion 给小怪 3。</summary>
    private int MaxHitsPerCard => Math.Max(1, (int)Amount);

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private WallState State => States.GetOrCreateValue(Owner);

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new StringVar("ImmuneCards");
            yield return new DynamicVar("MaxHits", 1);
        }
    }

    private void UpdateImmuneCardsDisplay()
    {
        var immune = State.ImmuneCardIds;
        var cardTitles = string.Join("\n", immune.Select(id =>
        {
            var card = ModelDb.GetById<CardModel>(id);
            return card != null ? "- " + card.Title : "- " + id.Entry;
        }));
        if (string.IsNullOrEmpty(cardTitles))
            cardTitles = "- 无";

        ((StringVar)DynamicVars["ImmuneCards"]).StringValue = cardTitles;
        DynamicVars["MaxHits"].BaseValue = MaxHitsPerCard;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        Entry.Logger.Info($"[HeartWall] applied owner={Owner?.Name} amount={Amount} instance={RuntimeHelpers.GetHashCode(this)}");
        var state = State;
        state.ImmuneCardIds.Clear();
        state.Attacks.Clear();
        state.CurrentPlayDamaged = false;
        DynamicVars["MaxHits"].BaseValue = MaxHitsPerCard;
        UpdateImmuneCardsDisplay();
    }

    // 免疫判定：只对 Owner 自己生效，只看 Owner 自己的免疫表
    public override decimal ModifyHpLostAfterOsty(
        Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || amount <= 0) return amount;
        if (cardSource != null && State.ImmuneCardIds.Contains(cardSource.Id))
            return 0;
        return amount;
    }

    // 一次「攻击行为」= 打出一张牌并造成伤害；这张牌有多少段伤害都只记 1 次。
    // BeforeCardPlayed 先清当前标记，AfterDamageReceived 只标记「本张牌造成过伤害」，
    // AfterCardPlayed 时再 +1；达到上限后，这张牌之后的攻击才免疫。
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        State.CurrentPlayDamaged = false;
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target == Owner && result.UnblockedDamage > 0 && cardSource != null)
            State.CurrentPlayDamaged = true;
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var state = State;
        var damaged = state.CurrentPlayDamaged;
        state.CurrentPlayDamaged = false;
        if (!damaged || cardPlay.Card == null) return Task.CompletedTask;

        var id = cardPlay.Card.Id;
        var attacks = state.Attacks.GetValueOrDefault(id) + 1;
        state.Attacks[id] = attacks;

        if (attacks >= MaxHitsPerCard && state.ImmuneCardIds.Add(id))
        {
            Entry.Logger.Info($"[HeartWall] owner={Owner?.Name} immune card={id.Entry} after {attacks} attacks");
            UpdateImmuneCardsDisplay();
        }
        return Task.CompletedTask;
    }

    // 回合开始时清空记录（只清 Owner 自己的）
    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            var state = State;
            state.ImmuneCardIds.Clear();
            state.Attacks.Clear();
            state.CurrentPlayDamaged = false;
            UpdateImmuneCardsDisplay();
        }
    }

    private sealed class WallState
    {
        public readonly Dictionary<ModelId, int> Attacks = new();
        public readonly HashSet<ModelId> ImmuneCardIds = new();
        public bool CurrentPlayDamaged;
    }
}
