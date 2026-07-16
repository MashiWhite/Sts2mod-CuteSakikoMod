using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class FullAssault : CuteAnonCard
{
    public FullAssault() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<FullAssaultPower>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.EquippedChords.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var power = await PowerCmd.Apply<FullAssaultPower>(
            choiceContext, Owner.Creature, 1, Owner.Creature, this);
        if (power != null && IsUpgraded)
            power.SetUpgraded(true);
    }

    protected override void OnUpgrade()
    {
        // 升级效果由能力内部 _upgraded 控制
    }
}