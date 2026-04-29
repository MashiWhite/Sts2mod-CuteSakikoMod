using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class StageJitters() : CuteAnonCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 基础伤害（未升级15，升级后18）
            yield return new CalculationBaseVar(15m);
            // 每个储存的音符或和弦降低的伤害值（额外伤害）
            yield return new ExtraDamageVar(2m);
            // 动态计算最终伤害（支持染色）
            yield return new CalculatedDamageVar(ValueProp.Move)
                .WithMultiplier((card, _) =>
                {
                    if (card.Owner == null) return 0m;
                    var noteCount = MusicNoteManager.GetCurrentNotes(card.Owner).Count;
                    var chordCount = MusicNoteManager.GetStoredChords(card.Owner).Count;
                    // 倍率 = -(音符总数 + 和弦总数)，使伤害降低
                    return -(decimal)(noteCount + chordCount);
                });
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        TriggerBanter();

        // 使用计算后的伤害值（会自动应用染色和实时计算）
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 基础伤害 15 → 18
        DynamicVars.CalculationBase.UpgradeValueBy(3m);
    }
}