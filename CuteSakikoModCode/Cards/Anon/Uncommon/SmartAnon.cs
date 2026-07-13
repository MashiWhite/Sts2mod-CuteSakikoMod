using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class SmartAnon : CuteAnonCard
{
    public SmartAnon() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("Count", 1);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        int count = DynamicVars["Count"].IntValue;

        // 构建可学习和弦池（已排除基础初始和弦），再排除已学习的
        var pool = new List<string>();
        foreach (ChordCategory cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
            pool.AddRange(ChordManager.GetLearnableChordIds(cat));

        // 排除已学习和临时和弦
        var alreadyKnown = new HashSet<string>(guitar.GetLearnedChords());
        var available = pool.Where(id => !alreadyKnown.Contains(id) && !ChordManager.AllChords[id].IsTemporaryOnly).ToList();

        // 随机学习指定数量（不够则全学）
        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var toLearn = available.OrderBy(_ => rng.NextFloat()).Take(count).ToList();

        foreach (var chordId in toLearn)
            guitar.LearnChord(chordId);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Count"].UpgradeValueBy(1); // 1 → 2
    }
}