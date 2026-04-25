
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

// 新增

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;


public class EncourageAgain() : CuteSakikoModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory);
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
        await PowerCmd.ModifyAmount(choiceContext,targetPressure, -requiredPressure, Owner.Creature, this);

        // 4. 获取可选回忆卡牌（排除已消耗的）
        var exhaustedPile = PileType.Exhaust.GetPile(Owner);
        var exhaustedMemoryIds = exhaustedPile?.Cards
            .Where(card => card.CanonicalKeywords.Contains(CutesakiKeywords.Memory))
            .Select(card => card.Id)
            .ToHashSet() ?? new HashSet<ModelId>();

        var availableMemoryCards = ModelDb.AllCards
            .Where(card =>
                card.CanonicalKeywords.Contains(CutesakiKeywords.Memory) &&
                !exhaustedMemoryIds.Contains(card.Id))
            .ToList();

        if (availableMemoryCards.Count == 0)
            return;

        // 5. 弹出选择界面（点击卡片即确认）
        var prefs = new CardSelectorPrefs(
            new LocString("gameplay_ui", "CHOOSE_CARD"),
            1,
            1
        );
        // 不设置 RequireManualConfirmation，使用默认行为

        var selectedCards = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            availableMemoryCards,
            Owner,
            prefs
        );

        var selectedCardModel = selectedCards.FirstOrDefault();


        if (selectedCardModel == null)
        {

            return;
        }

        // 6. 通过 CardFactory 生成可用的卡牌实例
        var generatedCard = CardFactory.GetDistinctForCombat(
            Owner,
            new List<CardModel> { selectedCardModel },
            1,
            Owner.RunState.Rng.CombatCardGeneration
        ).FirstOrDefault();

        if (generatedCard == null)
        {
            return;
        }

        // 7. 如果需要升级（本卡升级后获得的回忆卡牌也升级？根据设计可选）
        if (IsUpgraded)
        {
            generatedCard.UpgradeInternal();
            generatedCard.FinalizeUpgradeInternal();
        }

        // 8. 加入手牌
        await CardPileCmd.AddGeneratedCardToCombat(generatedCard, PileType.Hand, Owner);
  
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}