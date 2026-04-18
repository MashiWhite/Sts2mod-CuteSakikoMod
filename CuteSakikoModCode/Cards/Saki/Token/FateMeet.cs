using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

// 引入 FreePowerPower

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;

[Pool(typeof(TokenCardPool))]
public class FateMeet() : CustomCardModel(1, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    // 回忆关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Memory];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 施加免费下一张能力牌的效果（1层）
        await PowerCmd.Apply<FreePowerPower>(Owner.Creature, 1, Owner.Creature, this);

        // 每次打出后费用永久增加1（本场战斗）
        EnergyCost.UpgradeBy(1);
    }

    // 回忆效果：被消耗时，再次获得免费效果
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card,
        bool causedByEthereal)
    {
        if (card != this) return;
        await PowerCmd.Apply<FreePowerPower>(Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：初始费用从1变为0
        EnergyCost.UpgradeBy(-1);
    }
}