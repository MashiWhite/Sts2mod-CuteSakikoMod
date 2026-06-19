using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class Sweep() : CuteSakikoModCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PressurePower>(5m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<SweepPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 先检查并处理骑士之剑
        var swordExists = false;
        var searchPiles = new[] { PileType.Hand, PileType.Draw, PileType.Discard };
        foreach (var pileType in searchPiles)
        {
            var pile = pileType.GetPile(Owner);
            if (pile != null && pile.Cards.Any(c => c is KnightSword))
            {
                swordExists = true;
                break;
            }
        }

        if (!swordExists)
        {
            // 没有骑士之剑：创建一张新的并加入手牌
            var newSword = CombatState.CreateCard<KnightSword>(Owner);
            if (IsUpgraded)
                CardCmd.Upgrade(newSword);
            await CardPileCmd.AddGeneratedCardToCombat(newSword, PileType.Hand, Owner);
        }

        // 2. 然后施加压力和横扫能力（剑已就位，可以吃到压力加成）
        var pressureAmount = DynamicVars["PressurePower"].IntValue;
        await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, pressureAmount, Owner.Creature, this);
        await PowerCmd.Apply<SweepPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PressurePower"].UpgradeValueBy(5); // 压力 5 → 10
        EnergyCost.UpgradeBy(-1); // 2 费 → 1 费
    }
}