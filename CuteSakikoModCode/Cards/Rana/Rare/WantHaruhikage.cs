using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;
using System.Linq;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Rare;

public class WantHaruhikage() : CuteRanaCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CutesakiKeywords.RanaLive.GetModCardKeyword()
    ];
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<RanaLivePower>();
            yield return HoverTipFactory.FromPower<LiveSweetPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 对所有敌人造成伤害
        var attackCommand = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 统计产生了未被格挡伤害的结果数（每个结果对应一个敌人受到的一次伤害）
        int unblockedCount = attackCommand.Results
            .SelectMany(hitResults => hitResults)
            .Count(result => result.UnblockedDamage > 0);

        if (unblockedCount > 0)
            await PowerCmd.Apply<RanaLivePower>(choiceContext, Owner.Creature, unblockedCount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m); 
    }
}