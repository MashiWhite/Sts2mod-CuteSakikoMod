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

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Ancient;
// 按稀有度放在 Ancient 文件夹

public class NoWork : CuteSakikoModCard
{
    // 构造函数：0 费，攻击，古代，目标任意敌人
    public NoWork() : base(0, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    // 始终带有消耗关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 动态变量：伤害和压力
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(16m, ValueProp.Move), // 基础伤害 16
        new PowerVar<PressurePower>(10m) // 基础压力层数 10
    ];

    // 悬停提示：显示压力能力说明
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    // 打出效果：造成伤害 + 施加压力
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 施加压力
        await PowerCmd.Apply<PressurePower>(cardPlay.Target, DynamicVars["PressurePower"].IntValue, Owner.Creature,
            this);
    }

    // 升级效果：伤害 +7 (16→23)，压力 +10 (10→20)
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(7m);
        DynamicVars["PressurePower"].UpgradeValueBy(10m);
    }
}