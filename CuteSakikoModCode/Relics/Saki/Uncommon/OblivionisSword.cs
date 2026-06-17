using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Uncommon;

public sealed class OblivionisSword : CuteSakiRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<KnightSword>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sword.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<PressurePower>();
            
        }
    }

    // 战斗开始时添加一张骑士之剑加入手牌
    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Creature.Side || combatState.RoundNumber != 1)
            return;

        var sword = combatState.CreateCard<KnightSword>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(sword, PileType.Hand, Owner);
        Flash();
    }

    // 当其他人（非拥有者）的压力增加时，提升自己所有骑士之剑的伤害
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is not PressurePower)
            return;
        if (amount <= 0)
            return;

        // 避免与 PressurePower 自身的增伤重复（拥有者自身的压力增加由能力内部处理）
        if (power.Owner == Owner.Creature)
            return;

        int delta = (int)amount;
        if (delta <= 0) return;

        var player = Owner;
        var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust };
        foreach (var pileType in piles)
        {
            var pile = pileType.GetPile(player);
            if (pile == null) continue;
            foreach (var card in pile.Cards)
            {
                if (card is KnightSword ks)
                    ks.DynamicVars.Damage.BaseValue += delta;
            }
        }

        Flash();
    }
}