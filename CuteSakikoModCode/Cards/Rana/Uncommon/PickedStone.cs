using System.Collections.Generic;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class PickedStone : CuteRanaCard
{
    public PickedStone() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PickedStoneTemporaryDexterity>(2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<DexterityPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int amount = DynamicVars["PickedStoneTemporaryDexterity"].IntValue;
        await PowerCmd.Apply<PickedStoneTemporaryDexterity>(
            choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PickedStoneTemporaryDexterity"].UpgradeValueBy(1m);
    }

    [RegisterPower]
    public sealed class PickedStoneTemporaryDexterity : ModTemporaryPowerTemplate
    {
        public override AbstractModel OriginModel => ModelDb.Card<PickedStone>();
        public override PowerModel InternallyAppliedPower => ModelDb.Power<DexterityPower>();
        public override PowerAssetProfile AssetProfile => this.PowerAssetProfile();
    }
}