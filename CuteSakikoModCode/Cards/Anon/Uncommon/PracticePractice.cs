
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class PracticePractice() : CuteAnonCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.NoNote];
    
    // 可无限次在营地强化
    public override int MaxUpgradeLevel => 999;

    // 动态变量：每次升级增加 1 个随机音符
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DynamicVar("Notes", 1); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();
        int amount = DynamicVars["Notes"].IntValue;
        var rng = Owner.RunState.Rng.CombatCardSelection;
        var noteTypes = new[] { CardType.Attack, CardType.Skill, CardType.Power };
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        // 逐个生成音符，每个音符都会经历完整的吉他处理流程：
        // 添加进队列、匹配和弦、储存、溢出/自动演奏、触发MessyPlay等
        for (int i = 0; i < amount; i++)
        {
            var type = rng.NextItem(noteTypes);
            await guitar.OnNoteGenerated(choiceContext, type);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Notes"].UpgradeValueBy(1);
    }
}