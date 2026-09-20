using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

public class Unsheathe() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Sword.GetModCardKeyword()];

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new PowerVar<PressurePower>(5m); }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<KnightSword>();
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获得压力
        int pressureAmount = DynamicVars["PressurePower"].IntValue;
        await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, pressureAmount, Owner.Creature, this);

        // 2. 手牌已有剑则跳过（避免与 SwordManager 重复）
        var swordKeyword = CutesakiKeywords.Sword.GetModCardKeyword();
        var hand = PileType.Hand.GetPile(Owner);
        if (hand != null && hand.Cards.Any(c => c != this && c is KnightSword))
            return;

        // 3. 从抽牌堆/弃牌堆/消耗堆拉剑
        foreach (var pileType in new[] { PileType.Draw, PileType.Discard, PileType.Exhaust })
        {
            var pile = pileType.GetPile(Owner);
            if (pile == null) continue;
            var swords = pile.Cards.Where(c => c is KnightSword).ToList();
            foreach (var sword in swords)
            {
                sword.RemoveFromCurrentPile();
                await CardPileCmd.Add(sword, PileType.Hand);
            }
        }

        // 4. 从遗忘堆拉剑
        var forgetPile = ForgetCardPile.Get(Owner);
        if (forgetPile != null)
        {
            var swords = forgetPile.Cards.Where(c => c is KnightSword).ToList();
            foreach (var sword in swords)
            {
                forgetPile.RemoveInternal(sword);
                await CardPileCmd.Add(sword, PileType.Hand);
            }
            if (swords.Count > 0)
                forgetPile.InvokeContentsChanged();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PressurePower"].UpgradeValueBy(3m); // 5 → 8
    }
}