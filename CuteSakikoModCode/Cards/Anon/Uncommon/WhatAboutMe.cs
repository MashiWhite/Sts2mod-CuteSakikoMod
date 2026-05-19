using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class WhatAboutMe() : CuteAnonCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(4m, ValueProp.Move);
            yield return new TotalHitsVar();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var combat = Owner.Creature.CombatState;
        if (combat == null) return;

        var notesGained = MusicNoteManager.GetNotesGainedThisTurn(Owner);
        var totalHits = 1 + notesGained;
        var damage = DynamicVars.Damage.BaseValue;

        // 统一使用一个 AttackCommand，通过 WithHitCount 指定总命中次数
        // 活力只会消耗 1 层，但会对所有命中生效
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .WithHitCount(totalHits)                     // 关键：合并所有命中
            .TargetingRandomOpponents(combat)            // 随机选择目标
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 4 → 6
    }

    private class TotalHitsVar : DynamicVar
    {
        public TotalHitsVar() : base("TotalHits", 1m) { }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
            bool runGlobalHooks)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            if (card.Owner != null)
            {
                var notesGained = MusicNoteManager.GetNotesGainedThisTurn(card.Owner);
                BaseValue = 1 + notesGained;
            }
        }
    }
}