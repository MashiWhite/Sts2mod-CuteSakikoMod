
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Rare;

public class EncoreWish : CuteRanaCard
{
    public EncoreWish() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 只有拥有莱芜爽（LiveSweetPower）时才能打出
    protected override bool IsPlayable =>
        Owner != null && Owner.Creature.HasPower<LiveSweetPower>();

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<LiveSweetPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 施加下回合获得莱芜爽的 Power
        await PowerCmd.Apply<EncoreNextTurnPower>(
            choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级后获得保留
        AddKeyword(CardKeyword.Retain);
    }
}