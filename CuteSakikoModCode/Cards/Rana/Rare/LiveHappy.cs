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
        new DamageVar(8m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<LiveSweetPower>();
        }
    }

    // 只有拥有“莱芜爽”能力时才可打出
    protected override bool IsPlayable => Owner != null && Owner.Creature.HasPower<LiveSweetPower>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = ResolveEnergyXValue();
        if (x <= 0) return;

        var combat = Owner.Creature.CombatState;
        var targetRng = Owner.RunState.Rng.CombatTargets;

        bool anyKill;
        do
        {
            anyKill = false;
            for (int i = 0; i < x; i++)
            {
                var enemies = combat.HittableEnemies.ToList();
                if (enemies.Count == 0) break;

                var target = enemies[targetRng.NextInt(enemies.Count)];
                var results = await CreatureCmd.Damage(choiceContext, target, DynamicVars.Damage, Owner.Creature, this);
                if (results.Any(r => r.WasTargetKilled))
                    anyKill = true;
            }
        } while (anyKill);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m); 
    }
}