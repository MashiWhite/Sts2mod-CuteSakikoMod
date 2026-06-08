using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class CatPet : CuteRanaCard
{
    public CatPet() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Exhaust;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 收集所有牌堆中的猫咪
        var allPiles = new[]
        {
            PileType.Hand,
            PileType.Draw,
            PileType.Discard,
            PileType.Exhaust
        };

        var nekocards = new List<CardModel>();

        foreach (var pileType in allPiles)
        {
            var pile = pileType.GetPile(Owner);
            if (pile == null) continue;

            var catsInPile = pile.Cards
                .Where(c => c.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword()))
                .ToList();

            nekocards.AddRange(catsInPile);
        }

        // 升级所有猫咪
        foreach (var card in nekocards)
        {
            if (card.IsUpgradable)
            {
                CardCmd.Upgrade(card);
            }
        }

        // 等待一帧让升级动画/UI更新（可选）
        await Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        // 费用变为 0
        EnergyCost.UpgradeBy(-1);
    }
}