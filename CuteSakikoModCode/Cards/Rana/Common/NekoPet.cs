using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class NekoPet : CuteRanaCard
{
    public NekoPet() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Exhaust;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
        }
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("Neko",1)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int addCount = DynamicVars["Neko"].IntValue;
        if (addCount <= 0) return;

        // 获取所有 Neko 卡模板
        var allNekoCards = ModelDb.CardPool<CuteSakikoTokenCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword()))
            .ToList();
        if (allNekoCards.Count == 0) return;

        var combatState = Owner.Creature.CombatState!;
        var rng = Owner.RunState.Rng.CombatCardGeneration;

        for (int i = 0; i < addCount; i++)
        {
            var template = rng.NextItem(allNekoCards);
            if (template != null)
            {
                var newCard = combatState.CreateCard(template, Owner);
                newCard.EnergyCost.SetThisCombat(0, true); // 本场战斗免费
                await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Draw, Owner,CardPilePosition.Random);
            }
        }
        
        // 收集所有牌堆中的猫咪
        var allPiles = new[]
        {
            PileType.Hand,
            PileType.Draw,
            PileType.Discard,
            PileType.Exhaust
        };

        var nekocards = new List<CardModel>();

        foreach (var pileType in allPiles)
        {
            var pile = pileType.GetPile(Owner);
            if (pile == null) continue;

            var catsInPile = pile.Cards
                .Where(c => c.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword()))
                .ToList();

            nekocards.AddRange(catsInPile);
        }

        // 升级所有猫咪
        foreach (var card in nekocards)
        {
            if (card.IsUpgradable)
            {
                CardCmd.Upgrade(card);
            }
        }

        // 等待一帧让升级动画/UI更新（可选）
        await Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Neko"].UpgradeValueBy(1);
    }
}