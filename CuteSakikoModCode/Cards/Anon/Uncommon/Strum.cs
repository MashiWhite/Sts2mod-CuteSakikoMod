using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class Strum() : CuteAnonCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.NoNote];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        // 准备三种音符的模板（规范实例）
        var templateAtk = ModelDb.Card<AtkNote>();
        var templateSkill = ModelDb.Card<SkillNote>();
        var templatePower = ModelDb.Card<PowerNote>();

        // 连续选择三次
        for (var i = 0; i < 3; i++)
        {
            // 每次选择前从模板克隆全新实例，并关联战斗状态
            var options = new List<CardModel>
            {
                combatState.CreateCard<AtkNote>(Owner),
                combatState.CreateCard<SkillNote>(Owner),
                combatState.CreateCard<PowerNote>(Owner)
            };

            var selected = await CardSelectCmd.FromChooseACardScreen(
                choiceContext,
                options,
                Owner
            );

            if (selected != null) await CardCmd.AutoPlay(choiceContext, selected, null);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}