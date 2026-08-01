
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Rare;

public class LiveHappy : CuteRanaCard
{
    public LiveHappy() : base(-1, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<LiveSweetPower>();
        }
    }

    protected override bool IsPlayable => Owner != null && Owner.Creature.HasPower<LiveSweetPower>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = ResolveEnergyXValue();
        if (x <= 0) return;

        var combat = Owner.Creature.CombatState;
        var targetRng = Owner.RunState.Rng.CombatTargets;
        var damage = DynamicVars.Damage.BaseValue;

        bool anyKill;
        do
        {
            anyKill = false;
            for (int i = 0; i < x; i++)
            {
                var enemies = combat.HittableEnemies.ToList();
                if (enemies.Count == 0) break;

                var target = enemies[targetRng.NextInt(enemies.Count)];

                // 每次攻击构建一个新的 AttackCommand，并传入 cardPlay
                var attackCmd = await DamageCmd.Attack(damage)
                    .FromCard(this)  // 注意第二个参数
                    .Targeting(target)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);

                // 检查是否有敌人被击杀
                if (attackCmd.Results.SelectMany(r => r).Any(dr => dr.WasTargetKilled))
                    anyKill = true;
            }
        } while (anyKill);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m); 
    }
}