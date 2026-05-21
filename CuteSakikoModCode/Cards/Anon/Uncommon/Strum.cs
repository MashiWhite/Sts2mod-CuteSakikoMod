using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class Strum() : CuteAnonCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust,CutesakiKeywords.NoNote.GetModKeywordCardKeyword()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        for (var i = 0; i < 3; i++)
        {
            var options = new List<CardModel>
            {
                combatState.CreateCard<AtkNote>(Owner),
                combatState.CreateCard<SkillNote>(Owner),
                combatState.CreateCard<PowerNote>(Owner)
            };

            var selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner);
            if (selected != null)
                // ★ 完全复用打牌逻辑，包括溢出和自动播放
                await guitar.OnNoteGenerated(choiceContext, selected.Type);

            foreach (var option in options)
                combatState.RemoveCard(option);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}