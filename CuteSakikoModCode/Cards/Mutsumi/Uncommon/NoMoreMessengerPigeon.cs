using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Uncommon;

/// <summary>
///     我不会再当传信鸽（小睦/Cucumber 蓝卡）：0c 技能，给予 1 层虚弱；
///     若自身拥有虚弱，则把自身虚弱一并转移给目标。消耗；升级后基础虚弱 2 层。
/// </summary>
public class NoMoreMessengerPigeon()
    : CuteMutsumiCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy), IMutsumiArchetypeCard
{
    public MutsumiArchetype Archetype => MutsumiArchetype.Cucumber;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WeakPower>(1m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var transfer = 0;
        var selfWeak = Owner.Creature.GetPower<WeakPower>();
        if (selfWeak != null && selfWeak.Amount > 0)
        {
            transfer = selfWeak.Amount;
            await PowerCmd.Remove(selfWeak);
        }

        var total = DynamicVars["WeakPower"].IntValue + transfer;
        if (total <= 0) return;

        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            cardPlay.Target,
            total,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["WeakPower"].UpgradeValueBy(1m);
    }
}
