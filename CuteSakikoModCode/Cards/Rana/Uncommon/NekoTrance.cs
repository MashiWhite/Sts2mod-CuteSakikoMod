using System.Collections.Generic;
using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class NekoTrance : CuteRanaCard
{
    public NekoTrance() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        // 动态变量：添加的猫咪数量（用于描述）
        new CardsVar(1) // 基础1张，升级后变为2张（在 OnUpgrade 中修改）
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<NekoTrancePower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int catCount = DynamicVars.Cards.IntValue; // 升级前1，升级后2

        // 1. 随机添加猫咪到手牌
        var allNekoCards = ModelDb.CardPool<CuteSakikoTokenCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword()))
            .ToList();

        if (allNekoCards.Count > 0)
        {
            var combatState = Owner.Creature.CombatState!;
            var rng = Owner.RunState.Rng.CombatCardGeneration;

            for (int i = 0; i < catCount; i++)
            {
                var template = rng.NextItem(allNekoCards);
                var catCard = combatState.CreateCard(template, Owner);
                await CardPileCmd.AddGeneratedCardToCombat(catCard, PileType.Hand, Owner);
            }
        }

        // 2. 施加 NekoTrancePower（1 层，使猫咪获得保留）
        await PowerCmd.Apply<NekoTrancePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：猫咪数量 +1（1→2），费用 -1（2→1）
        DynamicVars.Cards.UpgradeValueBy(1);
        EnergyCost.UpgradeBy(-1);
    }
}