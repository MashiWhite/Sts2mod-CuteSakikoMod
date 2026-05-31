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

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Rare;

public class NekoMarch : CuteRanaCard
{
    public NekoMarch() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
        }
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(5m, ValueProp.Move)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 对所有敌人造成伤害
        int damage = (int)DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);

        // 2. 从所有牌堆中打出所有猫咪
        await PlayAllCatsFromAllPiles(choiceContext, Owner);
    }

    private async Task PlayAllCatsFromAllPiles(PlayerChoiceContext choiceContext, Player player)
    {
        var targetKeyword = CutesakiKeywords.Neko.GetModCardKeyword();
        var pileOrder = new[] { PileType.Exhaust, PileType.Discard, PileType.Draw, PileType.Hand };
        var allNekoCards = new List<CardModel>();

        // 按优先级收集所有猫咪
        foreach (var pileType in pileOrder)
        {
            var pile = pileType.GetPile(player);
            if (pile == null) continue;
            var cards = pile.Cards.Where(c => c.Keywords.Contains(targetKeyword)).ToList();
            allNekoCards.AddRange(cards);
        }

        if (allNekoCards.Count == 0) return;

        // 随机打乱顺序
        var rng = player.RunState.Rng.CombatCardSelection;
        var toPlay = allNekoCards.OrderBy(_ => rng.NextInt()).ToList();

        var combatState = player.Creature.CombatState!;
        foreach (var card in toPlay)
        {
            // 临时设为 0 费
            card.EnergyCost.SetThisTurnOrUntilPlayed(0);
            card.SetStarCostThisTurn(0);
            var target = SelectTarget(card, player, combatState);
            await CardCmd.AutoPlay(choiceContext, card, target);
            // 猫咪自带 Exhaust 关键词，打出后自动进入消耗堆，无需手动移除
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
        DynamicVars.Damage.UpgradeValueBy(5m); // 5 -> 10
    }
}