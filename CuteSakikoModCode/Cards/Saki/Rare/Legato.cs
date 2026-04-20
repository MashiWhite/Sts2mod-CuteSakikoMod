using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;


public class Legato : CuteSakikoModCard
{
    public Legato() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }
    

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            int baseDamage = 7; // 基础伤害固定7，升级不变
            int extraPerQin = IsUpgraded ? 6 : 4;

            yield return new CalculationBaseVar(baseDamage);
            yield return new ExtraDamageVar(extraPerQin);
            yield return new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, target) =>
            {
                var owner = card.Owner;
                if (owner == null) return 0m;
                int qinCount = owner.PlayerCombatState.AllCards.Count(c => c.Keywords.Contains(CutesakiKeywords.Playpiano));
                return qinCount;
            });
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Playpiano);
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

        int playCount = IsUpgraded ? 2 : 1;
        for (int i = 0; i < playCount; i++)
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
        var qin = discard?.Cards.FirstOrDefault(c => c.Keywords.Contains(CutesakiKeywords.Playpiano));
        if (qin != null) return qin;

        var draw = PileType.Draw.GetPile(player);
        qin = draw?.Cards.FirstOrDefault(c => c.Keywords.Contains(CutesakiKeywords.Playpiano));
        if (qin != null) return qin;

        var hand = PileType.Hand.GetPile(player);
        return hand?.Cards.FirstOrDefault(c => c.Keywords.Contains(CutesakiKeywords.Playpiano));
    }

    protected override void OnUpgrade()
    {
        // 升级效果已在 CanonicalVars 中通过 IsUpgraded 处理
    }
}