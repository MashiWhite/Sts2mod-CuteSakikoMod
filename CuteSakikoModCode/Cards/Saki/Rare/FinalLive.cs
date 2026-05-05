using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class FinalLive() : CuteSakikoModCard(3, CardType.Attack, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Playpiano);
        }
    }
    
    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.Playpiano];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new BlockVar(4m, ValueProp.Move) // 仅用于描述，实际动态使用
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var allCards = new List<CardModel>();

        var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard };
        foreach (var pileType in piles)
        {
            var pile = pileType.GetPile(owner);
            if (pile != null)
                allCards.AddRange(pile.Cards.Where(c => c.HasModKeyword(CutesakiKeywords.Playpiano)));
        }

        allCards = allCards.Distinct().ToList();

        var count = allCards.Count;
        if (count <= 0) return;

        var valueDamage = DynamicVars.Damage.BaseValue;
        var valueBlock = DynamicVars.Block.BaseValue;
        var totalDamage = count * valueDamage;
        var totalBlock = count * valueBlock;

        if (totalDamage > 0)
            await DamageCmd.Attack(totalDamage)
                .FromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

        if (totalBlock > 0)
            await CreatureCmd.GainBlock(owner.Creature, new BlockVar(totalBlock, ValueProp.Move), cardPlay);

        foreach (var card in allCards)
            if (card.Pile != null && card.Pile.IsCombatPile)
                await CardCmd.Exhaust(choiceContext, card);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        AddKeyword(CardKeyword.Innate);
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}