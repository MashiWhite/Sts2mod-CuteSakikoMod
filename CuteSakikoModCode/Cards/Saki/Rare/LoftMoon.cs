using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class LoftMoon() : CuteSakikoModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    // 关键词：保留（句首）、消耗（句末）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    // 定义动态变量：能量（2点）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(2)
    ];

    // 打出时获得2点能量
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(2, Owner);
    }

    // 当持有者将要死亡时，如果这张卡在手牌中，则阻止死亡并回复生命，然后消耗这张卡
    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner?.Creature) return base.ShouldDie(creature);
        var handPile = PileType.Hand.GetPile(Owner);
        if (handPile == null || !handPile.Cards.Contains(this))
            return base.ShouldDie(creature);
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner?.Creature) return;
        var handPile = PileType.Hand.GetPile(Owner);
        if (handPile == null || !handPile.Cards.Contains(this)) return;

        var healPercent = IsUpgraded ? 0.3m : 0.1m;
        var healAmount = Math.Max(1, (int)(creature.MaxHp * healPercent));
        await CreatureCmd.Heal(creature, healAmount);
        await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}