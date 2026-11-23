using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Planting;

/// <summary>
///     黄瓜：种下时是幼苗（3 伤害 × 1 段），经过 1 个玩家回合后成熟（3 伤害 × 2 段）。
///     黄瓜自身升级时额外 +1 段，不改变每段伤害；种小黄瓜的升级只加数量，不会升级黄瓜。
/// </summary>
public class Cucumber() : PlantingCardBase(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    public override int MaxStage => 2;

    protected override IReadOnlyList<string> StageNames => ["幼苗", "成熟"];

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Exhaust;
            yield return CutesakiKeywords.Planting.GetModCardKeyword();
        }
    }

    protected override decimal DamageForStage(int stage) => 3m;

    protected override int HitsForStage(int stage) => stage + (IsUpgraded ? 1 : 0);

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            foreach (var v in base.CanonicalVars) yield return v;
            yield return AttackHitCountVar.Create(1);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars["Repeat"].IntValue)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    /// <summary>悬浮提示里的下一阶段预览卡：用当前 RunState 新建一张黄瓜再切阶段。</summary>
    protected override PlantingCardBase? CreatePreviewCard()
    {
        return Owner?.RunState.CreateCard<Cucumber>(Owner);
    }
}
