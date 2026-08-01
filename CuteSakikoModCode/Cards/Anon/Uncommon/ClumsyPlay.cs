using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class ClumsyPlay() : CuteAnonCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Playguitar.GetModCardKeyword()];
    
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(7m, ValueProp.Move);
            yield return new BlockVar(7m, ValueProp.Move);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        TriggerBanter();

        // 造成伤害
        var damage = DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 获得格挡
        var block = DynamicVars.Block.IntValue;
        await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);

        // 演奏最新储存的和弦
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null)
            await guitar.TriggerLastStoredChord(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m); // 8 → 12
        DynamicVars.Block.UpgradeValueBy(3m); // 8 → 12
    }
}