using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Planting;

/// <summary>
///     种小黄瓜：1c 技能，向种植牌堆种下 1 张黄瓜（升级 2 张）。
///     种植堆满 3 张时不可打出（容量不足则该卡不可打出）。
/// </summary>
public class PlantCucumber() : CuteMutsumiCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Planting.GetModCardKeyword()];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // 卡组查看等界面拿到的是 canonical 模型，读 Owner 会抛 CanonicalModelException，
            // 导致 CreateHoverTips 中断（没提示框、也不会 MoveToFront）。
            if (IsCanonical)
            {
                yield return PlantingKeywordTip.Create(null);

                // 卡组查看里也显示黄瓜卡预览；FromCard 对 canonical 可能抛异常，兜住。
                IHoverTip? cucumberTip = null;
                try { cucumberTip = HoverTipFactory.FromCard<Cucumber>(); }
                catch { /* canonical 不支持时只显示关键词 */ }
                if (cucumberTip != null) yield return cucumberTip;
                yield break;
            }

            yield return PlantingKeywordTip.Create(Owner);
            yield return HoverTipFactory.FromCard<Cucumber>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];

    protected override bool IsPlayable
    {
        get
        {
            var count = (int)DynamicVars.Cards.BaseValue;
            return Owner != null && PlantingCardPile.CanPlant(Owner, count);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var count = (int)DynamicVars.Cards.BaseValue;
        var pileType = PlantingCardPile.GetPileType();
        var plantedTurn = Owner.PlayerCombatState?.TurnNumber ?? 0;

        for (var i = 0; i < count; i++)
        {
            var cucumber = CombatState.CreateCard<Cucumber>(Owner);
            if (cucumber is PlantingCardBase plant)
                plant.PlantedTurn = plantedTurn;
            await CardPileCmd.AddGeneratedCardToCombat(cucumber, pileType, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
