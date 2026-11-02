using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Rare;

/// <summary>
///     猫咪大冲击：消耗手牌中所有猫咪，每张对随机敌人造成 9 点伤害。
///     猫咪是消耗型衍生牌（打出即消耗），本卡把它们变成一次性爆发。
/// </summary>
public class NekoBigBang : CuteRanaCard
{
    public NekoBigBang() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 手牌中的猫咪逐张消耗，每张结算一次随机伤害
        var nekoKeyword = CutesakiKeywords.Neko.GetModCardKeyword();
        var nekons = PileType.Hand.GetPile(Owner)?
            .Cards.Where(c => c.Keywords.Contains(nekoKeyword))
            .ToList() ?? [];

        var damage = DynamicVars.Damage.BaseValue;
        foreach (var neko in nekons)
        {
            await DamageCmd.Attack(damage)
                .FromCard(this, cardPlay)
                .TargetingRandomOpponents(CombatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            await CardCmd.Exhaust(choiceContext, neko);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
