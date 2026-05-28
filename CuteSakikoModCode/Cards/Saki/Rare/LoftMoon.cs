using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class LoftMoon() : CuteSakikoModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(2, Owner);
    }

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

        // 使用合法上下文消耗自身
        var ctx = new HookPlayerChoiceContext(Owner, Owner.NetId, GameActionType.Combat);
        Task task = CardCmd.Exhaust(ctx, this);
        await ctx.AssignTaskAndWaitForPauseOrCompletion(task);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}