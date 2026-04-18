using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
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

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

[Pool(typeof(CuteSakiCardPool))]
public class StressResponse() : CustomCardModel(2, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move)
    ];

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
        var pressure = Owner.Creature.GetPower<PressurePower>();
        var layers = pressure?.Amount ?? 0;
        if (layers <= 0) return;

        // 计算每次伤害值 = 压力层数 / 4（至少1）
        var damagePerHit = Math.Max(1, layers / 4);

        // 消耗所有压力
        await PowerCmd.ModifyAmount(pressure, -layers, Owner.Creature, this);

        var hitCount = IsUpgraded ? 7 : 5;

        // 造成固定次数的随机伤害，每次伤害 = damagePerHit
        for (var i = 0; i < hitCount; i++)
            await DamageCmd.Attack(damagePerHit)
                .FromCard(this)
                .TargetingRandomOpponents(CombatState)
                .WithHitCount(1)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升级后伤害次数变为6，已在逻辑中处理
    }
}