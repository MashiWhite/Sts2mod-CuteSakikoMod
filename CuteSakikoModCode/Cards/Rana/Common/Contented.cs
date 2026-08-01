using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class Contented : CuteRanaCard
{
    public Contented() : base(3, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(40m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 结束莱芜爽：移除 LiveSweetPower
        if (Owner?.Creature.HasPower<LiveSweetPower>() == true)
        {
            var liveSweet = Owner.Creature.GetPower<LiveSweetPower>();
            if (liveSweet != null)
                await PowerCmd.Remove(liveSweet);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10m); // 40 → 50
    }
}