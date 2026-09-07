using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class PerfectPlay : CuteAnonCard
{
    public PerfectPlay() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.NoNote.GetModCardKeyword()];

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 随机演奏数量，升级后 +4
            yield return new DynamicVar("ChordCount", 6);
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.EquippedChords.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.LearnedChords.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        var learned = guitar.GetLearnedChords().ToList();
        if (learned.Count == 0) return;

        int targetCount = DynamicVars["ChordCount"].IntValue;

        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var selected = new List<string>(targetCount);
        for (int i = 0; i < targetCount; i++)
        {
            // 允许重复：随机从已学习和弦中抽取
            selected.Add(learned[rng.NextInt(learned.Count)]);
        }

        await guitar.PlaySpecificChords(choiceContext, selected);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
        DynamicVars["ChordCount"].UpgradeValueBy(4); // 6 → 10
    }
}