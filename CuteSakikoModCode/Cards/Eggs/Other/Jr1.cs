using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

public class Jr1() : ModTokenCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // 直接使用 FromCard<T> 的升级参数
            yield return HoverTipFactory.FromCard<Jr2>(IsUpgraded);
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar("lowdamage",8m, ValueProp.Move); // 单次高伤
            yield return new DamageVar(10m, ValueProp.Move); // 单次高伤
            yield return new RepeatVar( 2); // 两次低伤次数
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 两次低伤害（基础8，升级10）
        var lowDamage = DynamicVars["lowdamage"].IntValue;
        for (var i = 0; i < 2; i++)
            await DamageCmd.Attack(lowDamage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

        // 一次高伤害（基础10，升级13）
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 将致命连击2加入手牌（升级版本）
        var jr2 = CombatState.CreateCard<Jr2>(Owner);
        if (IsUpgraded && jr2.IsUpgradable)
            CardCmd.Upgrade(jr2);
        await CardPileCmd.AddGeneratedCardToCombat(jr2, PileType.Hand, Owner);

        await CardPileCmd.RemoveFromCombat(this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["lowdamage"].UpgradeValueBy(2);
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}