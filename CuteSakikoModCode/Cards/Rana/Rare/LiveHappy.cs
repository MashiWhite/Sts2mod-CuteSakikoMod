using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Rare;

public class LiveHappy : CuteRanaCard, CuteRanaCard.IEatParfaitCard
{
    public LiveHappy() : base(-1, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
    {
    }

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Parfait.GetModCardKeyword());
        }
    }

    public int GetParfaitConsumeCount() => 1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = ResolveEnergyXValue();
        if (x <= 0) return;

        var parfait = Owner.Relics.OfType<MatchaParfait>().FirstOrDefault();
        if (parfait == null) return;

        // 芭菲只消耗一次
        await MatchaParfait.RemoveCharges(parfait, 1, choiceContext);

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
                // 使用 DamageVar 重载
                var results = await CreatureCmd.Damage(choiceContext, target, DynamicVars.Damage, Owner.Creature, this);
                if (results.Any(r => r.WasTargetKilled))
                    anyKill = true;
            }
        } while (anyKill);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m); // 9 → 12
    }
}