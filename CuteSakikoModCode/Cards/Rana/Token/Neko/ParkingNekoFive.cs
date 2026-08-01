// 5. 罕见攻击：造成 4 点伤害，给予 1 层 VulnerablePower，升级后造成 8 点伤害

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Token.Neko;

public class ParkingNekoFive : NekoCard
{
    public ParkingNekoFive() : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(4m, ValueProp.Move),
        new PowerVar<VulnerablePower>(1m) // 易伤层数
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var damage = (int)DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        
        int vulnerablePower = DynamicVars["VulnerablePower"].IntValue;
        await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, vulnerablePower, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m); // 4 -> 8
    }
}