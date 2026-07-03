using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class StunningPerformance() : CuteAnonCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Playguitar.GetModCardKeyword()];
    
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            var baseDamage = 10;
            var extraPerNote = 1;

            yield return new CalculationBaseVar(baseDamage);
            yield return new ExtraDamageVar(extraPerNote);
            yield return new CalculatedDamageVar(ValueProp.Move)
                .WithMultiplier((card, target) =>
                {
                    var owner = card.Owner;
                    if (owner == null) return 0m;
                    // 改为使用本场战斗累计音符数
                    return MusicNoteManager.GetTotalNotesGainedThisCombat(owner);
                });
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this,cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 获取计算基础变量并升级：10 → 13
        var calcBase = DynamicVars["CalculationBase"];
        calcBase.UpgradeValueBy(3m);
        var extraPer = DynamicVars["ExtraDamage"];
        extraPer.UpgradeValueBy(1m);

        AddKeyword(CardKeyword.Retain);
    }
}