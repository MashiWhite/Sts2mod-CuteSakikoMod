using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;
using System.Linq;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class FinalLive() : CuteSakikoModCard(3, CardType.Attack, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Playpiano); }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, CutesakiKeywords.Playpiano.GetModCardKeyword()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new BlockVar(4m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var allCards = new List<CardModel>();

        var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard };
        var targetKeyword = CutesakiKeywords.Playpiano.GetModCardKeyword();

        foreach (var pileType in piles)
        {
            var pile = pileType.GetPile(owner);
            if (pile != null)
                allCards.AddRange(pile.Cards.Where(c => c.Keywords.Contains(targetKeyword)));
        }

        allCards = allCards.Distinct().ToList();

        var count = allCards.Count;
        if (count <= 0) return;

        // 1. 统一并发 Exhaust 所有符合条件的卡牌
        var exhaustTasks = allCards
            .Where(card => card.Pile != null && card.Pile.IsCombatPile)
            .Select(card => CardCmd.Exhaust(choiceContext, card));
        await Task.WhenAll(exhaustTasks);

        // 2. 获得格挡（基于 count）
        var valueBlock = DynamicVars.Block.BaseValue;
        var totalBlock = count * valueBlock;
        if (totalBlock > 0)
            await CreatureCmd.GainBlock(owner.Creature, new BlockVar(totalBlock, ValueProp.Move), cardPlay);

        // 3. 造成伤害（基于 count）
        var valueDamage = DynamicVars.Damage.BaseValue;
        var totalDamage = count * valueDamage;
        if (totalDamage > 0)
            await DamageCmd.Attack(totalDamage)
                .FromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        AddKeyword(CardKeyword.Innate);
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}