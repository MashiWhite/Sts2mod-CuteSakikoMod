using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

public class Jr2() : ModTokenCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DamageVar(IsUpgraded ? 13m : 9m, ValueProp.Move); }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // 直接使用 FromCard<T> 的升级参数
            yield return HoverTipFactory.FromCard<Jr3>(IsUpgraded);
            yield return HoverTipFactory.FromPower<FaintingPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 给予一层气绝
        await PowerCmd.Apply<FaintingPower>(choiceContext, cardPlay.Target, 1, Owner.Creature, this);

        // 将致命连击3加入手牌（升级版本）
        var jr3 = CombatState.CreateCard<Jr3>(Owner);
        if (IsUpgraded && jr3.IsUpgradable)
            CardCmd.Upgrade(jr3);
        await CardPileCmd.AddGeneratedCardToCombat(jr3, PileType.Hand, Owner);

        await CardPileCmd.RemoveFromCombat(this);
    }

    protected override void OnUpgrade()
    {
        // 升级效果在 DynamicVars 中处理
    }
}