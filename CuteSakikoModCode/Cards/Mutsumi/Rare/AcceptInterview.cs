using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Status;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Rare;

/// <summary>
///     接受采访（小睦金卡）：1c 技能。
///     获得 60 金币，将 2 张「筋疲力尽」加入弃牌堆；升级后改为 1 张。
/// </summary>
public class AcceptInterview()
    : CuteMutsumiCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self), IMutsumiArchetypeCard
{
    public MutsumiArchetype Archetype => MutsumiArchetype.Cucumber;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(60)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<Exhausted>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 升级效果在 OnPlay 的 IsUpgraded 分支处理，不覆写 OnUpgrade。
        await PlayerCmd.GainGold(DynamicVars.Gold.IntValue, Owner);

        var count = IsUpgraded ? 1 : 2;
        for (var i = 0; i < count; i++)
        {
            var exhausted = CombatState.CreateCard<Exhausted>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(exhausted, PileType.Discard, Owner);
        }
    }
}
