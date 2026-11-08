using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
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

        var notesGained = ChordNoteSystem.GetNotesGainedThisTurn(Owner);
        var totalHits = 1 + notesGained;
        var damage = DynamicVars.Damage.BaseValue;

        // 统一使用一个 AttackCommand，通过 WithHitCount 指定总命中次数
        // 活力只会消耗 1 层，但会对所有命中生效
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .WithHitCount(totalHits) // 关键：合并所有命中
            .TargetingRandomOpponents(combat) // 随机选择目标
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 4 → 6
    }

    private class TotalHitsVar : DynamicVar
    {
        public TotalHitsVar() : base("TotalHits", 1m)
        {
        }

        /// <summary>
        ///     命中数 = 1 + 本回合获得音符数。
        ///
        ///     ★ 变色规则（2026-11-09 修「数值提升但不变绿」）：
        ///     `:diff()` 走 HighlightDifferencesFormatter → DynamicVar.ToHighlightedString，
        ///     它比较的是 **PreviewValue（当前显示值） vs EnchantedValue（卡牌本身的值）**：
        ///         int value  = (int)PreviewValue;     // 显示出来的数
        ///         int value2 = (int)EnchantedValue;   // 基准（升级/附魔后"这张卡本来的值"）
        ///         CompareTo(value2) → 0=白 / 正=绿 / 负=红
        ///     旧写法直接改 BaseValue，导致 PreviewValue 与 EnchantedValue 恒等
        ///     → CompareTo 恒为 0 → 永远白色。而且 BaseValue 被覆盖后升级基准也丢失。
        ///     ⇒ 正确做法：EnchantedValue 留在"卡牌本来的值"（1 或升级后的值），
        ///        只把实时加总写进 PreviewValue，两者不等时才会变绿。
        /// </summary>
        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
            bool runGlobalHooks)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);

            // 基准 = 卡牌本来的命中数（本卡未升级，恒为 1）
            EnchantedValue = BaseValue;

            if (card.Owner == null) return;

            var notesGained = ChordNoteSystem.GetNotesGainedThisTurn(card.Owner);
            // 显示值 = 基准 + 本回合音符数（只影响 PreviewValue，不动 BaseValue）
            PreviewValue = BaseValue + notesGained;
        }
    }
}