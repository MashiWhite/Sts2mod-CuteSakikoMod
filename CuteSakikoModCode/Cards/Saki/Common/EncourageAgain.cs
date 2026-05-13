using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

public class EncourageAgain() : CuteSakikoModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Sakiforget);
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory);
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            var requiredPressure = IsUpgraded ? 2 : 3;
            return CombatState?.HittableEnemies?.Any(e =>
            {
                var p = e.GetPower<PressurePower>();
                return p != null && p.Amount >= requiredPressure;
            }) ?? false;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 检查目标压力
        var target = cardPlay.Target;
        var targetPressure = target.GetPower<PressurePower>();
        var requiredPressure = IsUpgraded ? 2 : 3;

        if (targetPressure == null || targetPressure.Amount < requiredPressure)
            return;

        // 3. 消耗压力
        await PowerCmd.ModifyAmount(choiceContext, targetPressure, -requiredPressure, Owner.Creature, this);

        // 4. 获取可选回忆卡牌规范模板
        var canonicalCards = MemoryCardPile.GetCanonicalCards(Owner);
        if (canonicalCards.Count == 0) return;
        
        // 5. 使用 CombatState.CreateCard 生成完全正确的战斗实例
        var combatReadyCards = canonicalCards
            .Select(template => Owner.Creature.CombatState.CreateCard(template, Owner))
            .ToList();
        
        // 6. 弹出选择界面
        var prefs = new CardSelectorPrefs(
            new LocString("cards", "CUTE_SAKIKO_MOD_CARD_ENCOURAGE_AGAIN.selectionScreenPrompt"),
            1,
            1
        );

        var selectedCards = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            combatReadyCards,
            Owner,
            prefs
        );

        var selectedCard = selectedCards.FirstOrDefault();
        if (selectedCard == null) return;

        // 7. 如需升级
        if (IsUpgraded)
        {
            selectedCard.UpgradeInternal();
            selectedCard.FinalizeUpgradeInternal();
        }

        // 8. 加入手牌
        await CardPileCmd.AddGeneratedCardToCombat(selectedCard, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}