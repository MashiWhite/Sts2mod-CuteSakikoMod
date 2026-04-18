using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Uncommon;

[Pool(typeof(CuteSakikoEggCardPool))]
public class HellBomb : CustomCardModel
{
    public HellBomb() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
    }


    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
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

        await CardPileCmd.AddGeneratedCardToCombat(bomb, PileType.Hand, true);

        // 永久删除牌库中的这张卡牌（整局游戏移除）
        if (this.DeckVersion != null)
        {
            await CardPileCmd.RemoveFromDeck(this.DeckVersion, true);
        }
        // 同时从战斗中移除当前实例（避免残留）
        await CardPileCmd.RemoveFromCombat(this);
    }

    protected override void OnUpgrade()
    {
        // 升级仅影响生成的炸弹
    }
}