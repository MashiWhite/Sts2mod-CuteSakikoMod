using CuteSakikoMod.CuteSakikoModCode.Cards.Anon;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class GuitarStrike() : CuteRanaCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Playguitar.GetModCardKeyword()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new("Count",2)
        
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        var count = DynamicVars["Count"].IntValue;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this,cardPlay)
            .WithHitCount(count)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Count"].UpgradeValueBy(1);
    }
}