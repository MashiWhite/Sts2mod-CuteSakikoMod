using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

[Pool(typeof(CuteSakiCardPool))]
public class Apathetic : CustomCardModel
{
    public Apathetic() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new BlockVar(10m, ValueProp.Move); }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<ArtifactPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡（使用升级后的值）
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 获得1层人工制品
        await PowerCmd.Apply<ArtifactPower>(Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：格挡 8 → 10
        DynamicVars.Block.UpgradeValueBy(3m);
        // 费用 1 → 0
        EnergyCost.UpgradeBy(-1);
    }
}