using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class MessyPlay() : CuteAnonCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 基础 1 层 MessyPlayPower，升级在 OnUpgrade 中 +1
            yield return new PowerVar<MessyPlayPower>(1m);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();
        var amount = DynamicVars["MessyPlayPower"].IntValue;
        await PowerCmd.Apply<MessyPlayPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：能力层数 +1 （1 → 2），本地化描述自动更新
        DynamicVars["MessyPlayPower"].UpgradeValueBy(1m);
        EnergyCost.UpgradeBy(-1); // 3 → 2
    }
}