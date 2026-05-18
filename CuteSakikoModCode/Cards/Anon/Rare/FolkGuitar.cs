using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class FolkGuitar : CuteAnonCard
{
    public FolkGuitar() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<FolkGuitarPower>();
            yield return HoverTipFactory.FromCard<PercussiveFingerstyle>(IsUpgraded);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        // 1. 施加能力
        var power = await PowerCmd.Apply<FolkGuitarPower>(
            choiceContext, Owner.Creature, 1, Owner.Creature, this);
        if (power != null && IsUpgraded)
            power.SetUpgraded(true);

        // 2. 立刻获得一张打板（跟随升级状态）
        var percussive = CombatState.CreateCard<PercussiveFingerstyle>(Owner);
        if (IsUpgraded)
        {
            percussive.UpgradeInternal();
            percussive.FinalizeUpgradeInternal();
        }

        await CardPileCmd.AddGeneratedCardToCombat(percussive, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
    }
}