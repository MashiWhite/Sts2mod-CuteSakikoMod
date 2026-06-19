using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class LiveWithYou() : CuteRanaCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CutesakiKeywords.RanaLive.GetModCardKeyword()
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<RanaLivePower>();
            yield return HoverTipFactory.FromPower<LiveSweetPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var targetPlayer = cardPlay.Target.Player;

        int teammateAmount = IsUpgraded ? 3 : 2;
        int selfAmount = IsUpgraded ? 1 : 0;

        // 给队友施加莱芜
        await PowerCmd.Apply<RanaLivePower>(choiceContext, targetPlayer.Creature, teammateAmount, Owner.Creature, this);

        // 给自己施加莱芜
        await PowerCmd.Apply<RanaLivePower>(choiceContext, Owner.Creature, selfAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级效果已在 IsUpgraded 中处理
    }
}