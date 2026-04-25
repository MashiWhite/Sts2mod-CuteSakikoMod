
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;


public class ThenGetSword() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly) // 目标改为任意队友
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 多人游戏限定
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<KnightSword>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sword);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var targetCreature = cardPlay.Target;
        if (targetCreature == null) return;
        var targetPlayer = targetCreature.Player;
        if (targetPlayer == null) return;

        // 为目标玩家生成一张骑士之剑
        var sword = CombatState.CreateCard<KnightSword>(targetPlayer);
        await CardPileCmd.AddGeneratedCardToCombat(sword, PileType.Hand, targetPlayer);
    }

    protected override void OnUpgrade()
    {
        // 升级：费用减少1（1→0）
        EnergyCost.UpgradeBy(-1);
    }
}