using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Oblivionis;

[RegisterCharacterStarterRelic(typeof(CuteOb))]
[RegisterTouchOfOrobasRefinement(typeof(ObHairBand))]
public class ObMask : CuteSakiRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    private int _triggeredRound = -1;

    // 每遗忘一张牌对所有敌人造成的伤害，进化后子类覆盖为6
    protected virtual int DamagePerForgottenCard => 3;

    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();
        MemoryCardPileManager.CardsForgotten += OnCardsForgotten;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        MemoryCardPileManager.CardsForgotten -= OnCardsForgotten;
        await base.AfterCombatEnd(room);
    }

    protected virtual async Task OnCardsForgotten(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<CardModel> cards,
        CardModel? source)
    {
        if (Owner == null || cards.Count == 0) return;
        if (cards[0].Owner != Owner) return;

        var combat = Owner.Creature?.CombatState;
        if (combat == null) return;

        // 伤害：每遗忘一张牌对所有敌人造成一次伤害
        await ApplyDamageForForgottenCards(choiceContext, cards);

        // 抽牌：每回合第一次遗忘时抽1张牌
        int round = combat.RoundNumber;
        if (_triggeredRound == round) return;
        _triggeredRound = round;

        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    /// <summary>
    /// 每遗忘一张牌，对所有敌人造成一次伤害。
    /// </summary>
    protected async Task ApplyDamageForForgottenCards(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<CardModel> cards)
    {
        if (cards.Count == 0) return;
        var combat = Owner?.Creature?.CombatState;
        if (combat == null) return;

        var enemies = combat.HittableEnemies;
        if (enemies.Count == 0) return;

        for (int i = 0; i < cards.Count; i++)
        {
            await CreatureCmd.Damage(
                choiceContext,
                enemies,
                new DamageVar(DamagePerForgottenCard, ValueProp.Unpowered),
                Owner.Creature,
                null,
                null
            );
        }
    }
}