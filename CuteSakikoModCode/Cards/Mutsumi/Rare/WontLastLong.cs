using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Rare;

/// <summary>
///     不会长久的（小睦/Cucumber 金卡）：1c 攻击。
///     消耗手牌中所有攻击牌，然后按实际消耗数量打出 10 点伤害；
///     升级后每次 13 点。
/// </summary>
public class WontLastLong()
    : CuteMutsumiCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy), IMutsumiArchetypeCard
{
    public MutsumiArchetype Archetype => MutsumiArchetype.Cucumber;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(10m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null) return;

        var attacks = hand.Cards
            .Where(c => c.Type == CardType.Attack)
            .ToList();

        var exhausted = 0;
        foreach (var card in attacks)
        {
            // 单张逐次消耗，保证每张的 AfterCardExhausted 钩子按顺序结算。
#if STS2_AT_LEAST_0_113_0
            if (await CardCmd.Exhaust(choiceContext, card) != null)
                exhausted++;
#else
            // 0.107/0.111 的 CardCmd.Exhaust 是 void，无法拿到结果；
            // 常规攻击牌均会消耗，按实际列表数量计。
            await CardCmd.Exhaust(choiceContext, card);
            exhausted++;
#endif
        }

        if (cardPlay.Target == null || exhausted <= 0) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(exhausted)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
