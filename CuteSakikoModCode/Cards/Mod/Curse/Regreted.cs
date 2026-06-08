
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Curse;

public class Regreted : ModCurseCard
{
    public Regreted() : base(1, CardType.Curse, CardRarity.Ancient, TargetType.Self)
    {
    }

    // 可被打出（重写基类 protected 虚属性）
    protected override bool IsPlayable => true;

    // 不可升级（CardModel 中 IsUpgradable 不是虚方法，通过 MaxUpgradeLevel = 0 实现）
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new HpLossVar(2m); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 抽 1 张牌
        await CardPileCmd.Draw(choiceContext, 1, Owner);

        // 对自己造成 2 点伤害
        VfxCmd.PlayOnCreatureCenter(Owner.Creature, "vfx/vfx_bloody_impact");
        await CreatureCmd.Damage(choiceContext, Owner.Creature,
            DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            this);
    }
}