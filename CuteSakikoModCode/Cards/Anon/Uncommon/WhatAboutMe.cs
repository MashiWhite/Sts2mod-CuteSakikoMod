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
            yield return new TotalHitsVar(); // 动态总攻击次数
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
        var rng = Owner.RunState.Rng.CombatCardSelection;

        for (var i = 0; i < totalHits; i++)
        {
            var hittable = combat.HittableEnemies;
            if (!hittable.Any()) break;
            var target = rng.NextItem(hittable);
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 4 → 6
    }

    // 嵌套动态变量：总攻击次数（1 + 本回合已获得音符数）
    private class TotalHitsVar : DynamicVar
    {
        public TotalHitsVar() : base("TotalHits", 1m)
        {
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
            bool runGlobalHooks)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            if (card.Owner != null)
            {
                var notesGained = MusicNoteManager.GetNotesGainedThisTurn(card.Owner);
                BaseValue = 1 + notesGained; // 基础1次 + 额外次数
            }
        }
    }
}