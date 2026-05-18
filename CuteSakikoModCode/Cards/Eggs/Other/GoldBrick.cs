using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

public class GoldBrick() : ModTokenCard(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new PowerVar<GoldPower>(0m); // 显示当前黄金层数
            yield return new DynamicVar("Multiplier", 1); // 伤害倍数，升级后改为 2
            yield return new GoldBrickDamageVar(); // 实时总伤害（战斗内有效）
        }
    }

    // 黄金不足 30 层时不可打出
    protected override bool IsPlayable
    {
        get
        {
            var gold = Owner?.Creature?.GetPower<GoldPower>();
            return gold != null && gold.Amount >= 30;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        var gold = Owner.Creature.GetPower<GoldPower>();
        if (gold == null || gold.Amount < 30) return;

        // 记录消耗前的黄金层数，用于计算伤害
        var goldAmountBefore = gold.Amount;

        // 消耗 30 层黄金
        await PowerCmd.ModifyAmount(choiceContext, gold, -30, Owner.Creature, this);

        // 伤害 = 消耗前的层数 × 倍数
        var mult = (int)DynamicVars["Multiplier"].BaseValue;
        var damage = goldAmountBefore * mult;
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 眩晕：将敌人本回合行动变为 STUNNED
        target.StunInternal(_ => Task.CompletedTask, null);
    }

    protected override void OnUpgrade()
    {
        // 升级后伤害倍率变为 2
        DynamicVars["Multiplier"].BaseValue = 2;
    }

    /// <summary>
    ///     动态变量：战斗内显示实际伤害 = 黄金层数 × 倍数，战斗外显示公式占位符（黄金层数*倍数）
    /// </summary>
    private class GoldBrickDamageVar : DynamicVar
    {
        public GoldBrickDamageVar() : base("TotalDamage", 0m)
        {
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
            bool runGlobalHooks)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            if (card.Owner == null) return;

            var goldPower = card.Owner.Creature?.GetPower<GoldPower>();
            var mult = (int)card.DynamicVars["Multiplier"].BaseValue;
            // 战斗内显示实际伤害，战斗外显示公式（BaseValue 留 0，描述通过 {InCombat:{TotalDamage:diff()}|{GoldPower}*{Multiplier}} 自动切换）
            if (goldPower != null && card.CombatState != null)
                BaseValue = goldPower.Amount * mult;
        }
    }
}