
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;
// 改为 Uncommon 命名空间


public class HeartAtk : CuteSakikoModCard
{
    public HeartAtk() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) // 稀有度改为 Uncommon
    {
    }

    // 基础版本有消耗关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move)
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
        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 获取目标身上的压力能力
        var pressure = cardPlay.Target.GetPower<PressurePower>();
        if (pressure != null && pressure.Amount > 0)
            // 压力层数翻倍（将当前值作为增加量，即 amount += amount）
            await PowerCmd.ModifyAmount(choiceContext,pressure, pressure.Amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：伤害增加5
        DynamicVars.Damage.UpgradeValueBy(5m);
        // 升级后去除消耗关键词
        RemoveKeyword(CardKeyword.Exhaust);
    }
}