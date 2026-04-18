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

[Pool(typeof(CuteSakiCardPool))]
public class StrikeSlow() : CustomCardModel(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Playpiano];

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new BlockVar(2m, ValueProp.Move) // 仅用于描述，实际动态使用
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
        var extraBlock = DynamicVars.Block.BaseValue;

        var pressure = Owner.Creature.GetPower<PressurePower>();
        var hasPressure = pressure != null && pressure.Amount > 0;

        if (hasPressure)
        {
            // 消耗1层压力
            await PowerCmd.ModifyAmount(pressure, -1, Owner.Creature, this);
            
        }

        // 造成伤害
        await DamageCmd.Attack(baseDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 获得格挡（如果有）
        if (extraBlock > 0)
            await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(extraBlock, ValueProp.Move), cardPlay);
    }

    protected override void OnUpgrade()
    {
       DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(3m); 
    }
}