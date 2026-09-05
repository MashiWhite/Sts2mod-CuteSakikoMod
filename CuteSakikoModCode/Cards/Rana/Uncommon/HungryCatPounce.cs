
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

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class HungryCatPounce : CuteRanaCard
{
    public HungryCatPounce() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 对所有敌人造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)                       // 注意：新版需要传入 cardPlay
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 从抽牌堆打出猫咪
        var drawPile = PileType.Draw.GetPile(Owner);
        if (drawPile == null) return;

        var nekoKeyword = CutesakiKeywords.Neko.GetModCardKeyword();
        var nekoCardsInDraw = drawPile.Cards
            .Where(c => c.Keywords.Contains(nekoKeyword))
            .ToList();
        if (nekoCardsInDraw.Count == 0) return;

        int enemyCount = CombatState.HittableEnemies.Count;
        int catsToPlay = Math.Min(enemyCount, nekoCardsInDraw.Count);
        if (catsToPlay <= 0) return;

        // 随机打乱后取前 catsToPlay 张
        var rng = Owner.RunState.Rng.CombatCardSelection;
        var selectedCats = nekoCardsInDraw
            .OrderBy(_ => rng.NextInt())
            .Take(catsToPlay)
            .ToList();

        foreach (var catCard in selectedCats)
        {
            catCard.EnergyCost.SetThisTurnOrUntilPlayed(0);
            catCard.SetStarCostThisTurn(0);
            var target = SelectTarget(catCard, Owner, CombatState);
            await CardCmd.AutoPlay(choiceContext, catCard, target);
        }
    }

    private Creature? SelectTarget(CardModel card, Player player, ICombatState combatState)
    {
        return card.TargetType switch
        {
            TargetType.Self => player.Creature,
            TargetType.AnyEnemy => combatState.HittableEnemies.Any()
                ? combatState.HittableEnemies.ElementAt(
                    player.RunState.Rng.CombatTargets.NextInt(combatState.HittableEnemies.Count))
                : null,
            TargetType.AllEnemies => null,
            _ => null
        };
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m); // 10 → 15
    }
}