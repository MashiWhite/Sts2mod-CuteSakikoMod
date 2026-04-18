using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

[Pool(typeof(CuteSakiCardPool))]
public class HateSelf() : CustomCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();


    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PressurePower>(5m),
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            // 返回压力能力的悬停提示
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            // 如果有其他提示，继续 yield return
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        
        // 施加压力
        await PowerCmd.Apply<PressurePower>(Owner.Creature, DynamicVars["PressurePower"].IntValue, Owner.Creature,
            this);

        // 抽牌
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        // 升级：压力增加2层（5->7），抽牌增加1张（1->2）
        DynamicVars["PressurePower"].UpgradeValueBy(2m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}