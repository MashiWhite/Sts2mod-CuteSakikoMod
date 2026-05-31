using CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Basic;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class CatchCat : CuteRanaCard
{
    public CatchCat() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(8m, ValueProp.Move)
    };
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 造成伤害
        if (cardPlay.Target != null)
        {
            int damage = (int)DynamicVars.Damage.BaseValue;
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }

        // 从所有牌堆中打出猫咪（升级前1只，升级后2只）
        int catCount = IsUpgraded ? 2 : 1;
        await PlayCatsFromAllPiles(choiceContext, Owner, catCount);
    }

    private async Task PlayCatsFromAllPiles(PlayerChoiceContext choiceContext, Player player, int count)
    {
        if (count <= 0) return;

        var targetKeyword = CutesakiKeywords.Neko.GetModCardKeyword();
        // 优先级顺序：Exhaust → Discard → Draw → Hand
        var pileOrder = new[] { PileType.Exhaust, PileType.Discard, PileType.Draw, PileType.Hand };
        var allNekoCards = new List<CardModel>();

        foreach (var pileType in pileOrder)
        {
            if (allNekoCards.Count >= count) break;
            var pile = pileType.GetPile(player);
            if (pile == null) continue;
            var cards = pile.Cards.Where(c => c.Keywords.Contains(targetKeyword)).ToList();
            if (cards.Count == 0) continue;
            int need = count - allNekoCards.Count;
            if (cards.Count <= need)
                allNekoCards.AddRange(cards);
            else
                allNekoCards.AddRange(cards.Take(need));
        }

        if (allNekoCards.Count == 0) return;

        // 随机打乱顺序
        var rng = player.RunState.Rng.CombatCardSelection;
        var toPlay = allNekoCards.OrderBy(_ => rng.NextInt()).ToList();

        var combatState = player.Creature.CombatState!;
        foreach (var card in toPlay)
        {
            card.EnergyCost.SetThisTurnOrUntilPlayed(0);
            card.SetStarCostThisTurn(0);
            var target = SelectTarget(card, player, combatState);
            await CardCmd.AutoPlay(choiceContext, card, target);
            // 猫咪自带 Exhaust 关键词，打出后自动进入消耗堆，不手动移除
        }
    }

    private Creature? SelectTarget(CardModel card, Player player, ICombatState combatState)
    {
        return card.TargetType switch
        {
            TargetType.Self => player.Creature,
            TargetType.AnyEnemy => combatState.HittableEnemies.Any()
                ? combatState.HittableEnemies.ElementAt(player.RunState.Rng.CombatTargets.NextInt(combatState.HittableEnemies.Count()))
                : null,
            TargetType.AllEnemies => null,
            _ => null
        };
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 8 -> 10
    }
}