using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Basic;


public class StrikeFast() : CuteSakikoModCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Playpiano];

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new DamageVar(6m, ValueProp.Move),
        new DamageVar("ExtraDamage",3m, ValueProp.Move)
    ];

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            var pressure = Owner.Creature.GetPower<PressurePower>();
            return pressure != null && pressure.Amount >= 1;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var baseDamage = DynamicVars.Damage.BaseValue;
        var extraDamage = ((DamageVar)DynamicVars["ExtraDamage"]).BaseValue;

        var pressure = Owner.Creature.GetPower<PressurePower>();
        var hasPressure = pressure != null && pressure.Amount > 0;

        if (cardPlay.Target != null)
        {
            // 基础攻击
            await DamageCmd.Attack(baseDamage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            // 压力触发额外攻击
            if (hasPressure)
            {
                // 消耗1层压力
                await PowerCmd.ModifyAmount(pressure, -1, Owner.Creature, this);

                if (IsUpgraded)
                    // 升级后：两次2点伤害
                    for (var i = 0; i < 2; i++)
                        await DamageCmd.Attack(extraDamage)
                            .FromCard(this)
                            .Targeting(cardPlay.Target)
                            .WithHitFx("vfx/vfx_attack_slash")
                            .Execute(choiceContext);
                else
                    // 未升级：一次3点伤害
                    await DamageCmd.Attack(extraDamage)
                        .FromCard(this)
                        .Targeting(cardPlay.Target)
                        .WithHitFx("vfx/vfx_attack_slash")
                        .Execute(choiceContext);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        ((DamageVar)DynamicVars["ExtraDamage"]).UpgradeValueBy(-1m);
    }
}