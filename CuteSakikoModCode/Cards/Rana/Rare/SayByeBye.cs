using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Rare;

/// <summary>
///     讨厌的东西说拜拜：消耗手牌中所有诅咒与状态牌，每消耗一张抽 1 张牌。
///     升级：添加保留。
/// </summary>
public class SayByeBye : CuteRanaCard
{
    public SayByeBye() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var garbage = PileType.Hand.GetPile(Owner)?
            .Cards.Where(c =>
                c.Type == CardType.Curse ||
                c.Type == CardType.Status)
            .ToList() ?? [];

        var drawn = 0;
        foreach (var card in garbage)
        {
            await CardCmd.Exhaust(choiceContext, card);
            drawn++;
        }

        if (drawn > 0)
            await CardPileCmd.Draw(choiceContext, drawn, Owner);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
