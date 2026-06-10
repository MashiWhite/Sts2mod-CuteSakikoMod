
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class NobodyKnowParfait : CuteRanaCard, CuteRanaCard.IEatParfaitCard
{
    public NobodyKnowParfait() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new BlockVar(16m, ValueProp.Move); }
    }

    public int GetParfaitConsumeCount() => 1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 消耗 1 杯抹茶芭菲
        var parfait = Owner.Relics.OfType<MatchaParfait>().FirstOrDefault();
        MatchaParfait.RemoveCharges(parfait, 1, choiceContext);

        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m); // 19
    }
}