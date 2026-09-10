using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Ancient;

public class PlayImmediately() : CuteAnonCard(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.NoNote.GetModCardKeyword()];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new ("Chords",1),
        new RepeatVar(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<PlayImmediatelyPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var count = DynamicVars.Repeat.IntValue;
        // 激活音符系统（即使没有吉他）
        ChordNoteSystem.Activate(Owner);
        
        // 播放所有储存的和弦，保留音符但清空储存和弦
        await ChordNoteSystem.PlayAllStoredChordsAsync(Owner, choiceContext, countPerChord: count);
        
        var chords = DynamicVars["Chords"].BaseValue;
        await PowerCmd.Apply<PlayImmediatelyPower>(choiceContext, Owner.Creature, chords, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Chords"].UpgradeValueBy(2);
        DynamicVars.Repeat.UpgradeValueBy(1);
    }
}