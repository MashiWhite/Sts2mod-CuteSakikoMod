using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class GiveCandy() : CuteAnonCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    // 多人游戏限定
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    // 关键词：消耗
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    // 动态变量：能量和抽牌（基础均为2）
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new EnergyVar(2);
            yield return new CardsVar(2);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var targetPlayer = cardPlay.Target.Player;

        // 为目标队友增加能量
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, targetPlayer);

        // 为目标队友抽牌
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, targetPlayer);
    }

    protected override void OnUpgrade()
    {
        // 升级：能量 2→3，抽牌 2→3
        DynamicVars.Energy.UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}