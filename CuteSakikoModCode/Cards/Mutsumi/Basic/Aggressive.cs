using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Basic;

/// <summary>
///     咄咄逼人（Mortis 初始卡）：1c，造成 8 点伤害；
///     返还本张牌消耗的「言刃」层数；升级后额外获得 2 层。
/// </summary>
public class Aggressive() : CuteMutsumiCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy),
    IMutsumiArchetypeCard
{
    public MutsumiArchetype Archetype => MutsumiArchetype.Mortis;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<WordBladePower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var speechBefore = Owner.Creature.GetPower<WordBladePower>();
        speechBefore?.ResetLastConsumed();

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (Owner.Creature.CombatState == null) return;

        var speechAfter = Owner.Creature.GetPower<WordBladePower>();
        // 言刃被消耗到 0 层时能力会被移除，speechAfter 会变成 null；
        // 必须读攻击前持有的实例，它在 AfterAttack 里记录了 LastConsumed。
        var consumed = speechBefore?.LastConsumed ?? 0;
        var gain = consumed + (IsUpgraded ? 2 : 0);

        if (gain <= 0) return;

        await PowerCmd.Apply<WordBladePower>(choiceContext, Owner.Creature, gain, Owner.Creature, null);
    }
}
