using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class MendedGrudge : CuteAnonCard
{
    public MendedGrudge() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Retain;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var combat = Owner.Creature.CombatState;
        if (combat == null) return;

        // 对所有敌人造成伤害
        var enemies = combat.Enemies;
        if (enemies != null && enemies.Any())
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .TargetingAllOpponents(combat)            // 修正为全体目标
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        // 收集并移除手牌中的所有状态卡
        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null) return;

        var statusCards = hand.Cards.Where(c => c.Type == CardType.Status).ToList();
        if (statusCards.Count == 0) return;

        await CardPileCmd.RemoveFromCombat(statusCards);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}