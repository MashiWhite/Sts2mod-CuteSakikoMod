using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class BrokenPick() : CuteAnonCard(2, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DynamicVar("BlockPerNote", 5m); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var noteCount = MusicNoteManager.ClearNotesAndGetCount(Owner);
        var blockAmount = noteCount * DynamicVars["BlockPerNote"].IntValue;

        if (blockAmount > 0) await CreatureCmd.GainBlock(Owner.Creature, blockAmount, 0, null);

        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        guitar?.UpdateNoteDisplay();
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2费 → 1费
        AddKeyword(CardKeyword.Retain); // 添加保留
    }
}