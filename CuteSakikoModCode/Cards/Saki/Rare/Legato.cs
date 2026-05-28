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

public class Legato() : CuteSakikoModCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Playpiano); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new CalculationBaseVar(7);
            yield return new ExtraDamageVar(4);
            yield return new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, target) =>
            {
                var owner = card.Owner;
                if (owner == null) return 0m;
                var qinCount =
                    owner.PlayerCombatState.AllCards.Count(c =>
                        c.Keywords.Contains(CutesakiKeywords.Playpiano.GetModCardKeyword()));
                return qinCount;
            });
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var playCount = IsUpgraded ? 2 : 1;
        for (var i = 0; i < playCount; i++)
        {
            var qinCard = GetNextQinCard();
            if (qinCard == null) break;

            if (qinCard.Pile?.Type != PileType.Hand)
            {
                qinCard.RemoveFromCurrentPile();
                await CardPileCmd.Add(qinCard, PileType.Hand);
            }

            qinCard.ExhaustOnNextPlay = false;
            await CardCmd.AutoPlay(choiceContext, qinCard, cardPlay.Target);
        }
    }

    private CardModel? GetNextQinCard()
    {
        var player = Owner;
        var discard = PileType.Discard.GetPile(player);
        var qin =
            discard?.Cards.FirstOrDefault(c => c.Keywords.Contains(CutesakiKeywords.Playpiano.GetModCardKeyword()));
        if (qin != null) return qin;

        var draw = PileType.Draw.GetPile(player);
        qin = draw?.Cards.FirstOrDefault(c => c.Keywords.Contains(CutesakiKeywords.Playpiano.GetModCardKeyword()));
        if (qin != null) return qin;

        var hand = PileType.Hand.GetPile(player);
        return hand?.Cards.FirstOrDefault(c => c.Keywords.Contains(CutesakiKeywords.Playpiano.GetModCardKeyword()));
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(2);
    }
}