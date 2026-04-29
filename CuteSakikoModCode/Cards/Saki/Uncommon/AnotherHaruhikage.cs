using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Status;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class AnotherHaruhikage() : CuteSakikoModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(13m, ValueProp.Move),
        new PowerVar<PressurePower>(10m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            // 根据自身升级状态显示对应的 Noise 卡牌
            yield return HoverTipFactory.FromCard<Noise>(IsUpgraded);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var damage = DynamicVars["Damage"].IntValue;
        var pressureAmount = DynamicVars["PressurePower"].IntValue;

        // 对所有敌人造成伤害并施加压力
        var enemies = CombatState?.HittableEnemies;
        if (enemies != null)
            foreach (var enemy in enemies)
            {
                await DamageCmd.Attack(damage)
                    .FromCard(this)
                    .Targeting(enemy)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);

                await PowerCmd.Apply<PressurePower>(choiceContext, enemy, pressureAmount, Owner.Creature, this);
            }

        // 用杂音填满手牌
        await FillHandWithNoise();
    }

    private async Task FillHandWithNoise()
    {
        var handPile = PileType.Hand.GetPile(Owner);
        if (handPile == null) return;

        var currentHandSize = handPile.Cards.Count;
        var maxHandSize = 10; // 手牌上限
        var needed = maxHandSize - currentHandSize;
        if (needed <= 0) return;

        for (var i = 0; i < needed; i++)
        {
            CardModel noise;
            if (IsUpgraded)
            {
                // 创建升级版 Noise+
                noise = CombatState.CreateCard<Noise>(Owner);
                CardCmd.Upgrade(noise);
            }
            else
            {
                noise = CombatState.CreateCard<Noise>(Owner);
            }

            await CardPileCmd.AddGeneratedCardToCombat(noise, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：伤害 13 → 18，压力 5 → 10
        DynamicVars["Damage"].UpgradeValueBy(5m);
        DynamicVars["PressurePower"].UpgradeValueBy(5m);
    }
}