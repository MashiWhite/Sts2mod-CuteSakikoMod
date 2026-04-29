using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Ancient;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Basic;

[RegisterCharacterStarterCard(typeof(CuteSaki))]
[RegisterArchaicToothTranscendence(typeof(NoWork))]
public class GoWork() : CuteSakikoModCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    // 始终带有消耗关键词（升级后仍保留）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<PressurePower>(6m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // 返回压力能力的悬停提示
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            // 如果有其他提示，继续 yield return
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

        // 施加压力
        await PowerCmd.Apply<PressurePower>(choiceContext, cardPlay.Target, DynamicVars["PressurePower"].IntValue,
            Owner.Creature,
            this);
    }

    public CardModel GetTranscendenceTransformedCard()
    {
        return ModelDb.Card<NoWork>();
    }

    protected override void OnUpgrade()
    {
        // 升级：伤害+2（8→10），压力+2（4→6）
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["PressurePower"].UpgradeValueBy(2m);
    }
}