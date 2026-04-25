
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;


public class MusicaCaelestis : CuteSakikoModCard
{
    public MusicaCaelestis() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }
    

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<MusicaCaelestisPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var existing = Owner.Creature.GetPower<MusicaCaelestisPower>();
        if (existing == null)
            await PowerCmd.Apply<MusicaCaelestisPower>(choiceContext,Owner.Creature, 1, Owner.Creature, this);
        else
            // 可叠加层数
            await PowerCmd.ModifyAmount(choiceContext,existing, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2 → 1
    }
}