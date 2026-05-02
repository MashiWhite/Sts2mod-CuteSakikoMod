using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class StunningPerformance() : CuteAnonCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            var baseDamage = 10;
            var extraPerNote = 2;

            yield return new CalculationBaseVar(baseDamage);              // 基础伤害
            yield return new ExtraDamageVar(extraPerNote);                // 每音符提升值
            yield return new CalculatedDamageVar(ValueProp.Move)
                .WithMultiplier((card, target) =>
                {
                    var owner = card.Owner;
                    if (owner == null) return 0m;
                    var notes = MusicNoteManager.GetNotesGainedThisTurn(owner);
                    return notes;
                });
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 获取计算基础变量并升级：10 → 13
        var calcBase = DynamicVars["CalculationBase"];
        calcBase.UpgradeValueBy(3m);

        // 获取额外伤害变量并升级：2 → 3
        var extra = DynamicVars["ExtraDamage"];
        extra.UpgradeValueBy(1m);
    }
}