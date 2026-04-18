using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

// 引入 VigorPower

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;

[Pool(typeof(TokenCardPool))]
public class PromiseOfGrowing() : CustomCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    // 回忆关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Memory];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<VigorPower>(3m) // 基础2层活力
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var amount = DynamicVars["VigorPower"].IntValue;
        await PowerCmd.Apply<VigorPower>(Owner.Creature, amount, Owner.Creature, this);

        // 每次打出后费用永久增加1（本场战斗）
        EnergyCost.UpgradeBy(1);
    }

    // 回忆效果：被消耗时，再次获得等量活力
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card,
        bool causedByEthereal)
    {
        if (card != this) return;
        var amount = DynamicVars["VigorPower"].IntValue;
        await PowerCmd.Apply<VigorPower>(Owner.Creature, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：活力层数 +1（2→3）
        DynamicVars["VigorPower"].UpgradeValueBy(3m);
    }
}