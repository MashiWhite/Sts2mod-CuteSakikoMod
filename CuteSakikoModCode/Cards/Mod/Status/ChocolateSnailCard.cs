
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Status;

public class ChocolateSnailCard : ModStatusCard
{
    public ChocolateSnailCard() : base(1, CardType.Skill, CardRarity.Status, TargetType.AllEnemies) { }

    // 不可升级
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var enemy in CombatState.Enemies)
        {
            await PowerCmd.Apply<DeliciousPower>(
                choiceContext, enemy, 5, Owner.Creature, this);
        }
    }
}