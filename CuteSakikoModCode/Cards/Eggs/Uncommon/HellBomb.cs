using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Uncommon;

public class HellBomb() : CuteSakikoModEggCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromCard<HellBombOn>(IsUpgraded); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        var targetPlayer = cardPlay.Target.Player;
        if (targetPlayer == null) return;

        var bomb = CombatState.CreateCard<HellBombOn>(targetPlayer);
        if (IsUpgraded) CardCmd.Upgrade(bomb);

        await CardPileCmd.AddGeneratedCardToCombat(bomb, PileType.Hand, Owner);

        // 永久删除牌库中的这张卡牌（整局游戏移除）
        if (DeckVersion != null) await CardPileCmd.RemoveFromDeck(DeckVersion);
        // 同时从战斗中移除当前实例（避免残留）
        await CardPileCmd.RemoveFromCombat(this);
    }

    protected override void OnUpgrade()
    {
        // 升级仅影响生成的炸弹
    }
}