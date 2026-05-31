// 4. 罕见技能：给予 1 层 NekoTempStrengthDownPower，升级后给予 2 层

using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Token.Neko;

public class ParkingNekoFour : NekoCard
{
    public ParkingNekoFour() : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new PowerVar<NekoTempStrengthDownPower>(1)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var amount = DynamicVars["NekoTempStrengthDownPower"].IntValue;
        // 假设 NekoTempStrengthDownPower 已定义，命名空间请根据实际情况调整
        await PowerCmd.Apply<NekoTempStrengthDownPower>(choiceContext, cardPlay.Target, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["NekoTempStrengthDownPower"].UpgradeValueBy(1);
    }
}