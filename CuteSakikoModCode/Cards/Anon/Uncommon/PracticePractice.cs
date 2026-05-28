using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class PracticePractice() : CuteAnonCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.NoNote.GetModCardKeyword()];

    // 可无限次在营地强化
    public override int MaxUpgradeLevel => 999;

    // 动态变量：初始 1 个音符，每次升级增加 CurrentUpgradeLevel 个
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DynamicVar("Notes", 1); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();
        var amount = DynamicVars["Notes"].IntValue;
        var rng = Owner.RunState.Rng.CombatCardSelection;
        var noteTypes = new[] { CardType.Attack, CardType.Skill, CardType.Power };
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        for (var i = 0; i < amount; i++)
        {
            var type = rng.NextItem(noteTypes);
            await guitar.OnNoteGenerated(choiceContext, type);
        }
    }

    protected override void OnUpgrade()
    {
        // 每次升级增加的音符数 = 2 ^ CurrentUpgradeLevel
        // 首次升级时 CurrentUpgradeLevel = 1 → +2, 第二次 = 2 → +4, 第三次 = 3 → +8 ...
        var additionalNotes = 1 << CurrentUpgradeLevel; // 等价于 (int)Math.Pow(2, CurrentUpgradeLevel)
        DynamicVars["Notes"].UpgradeValueBy(additionalNotes);
    }
}