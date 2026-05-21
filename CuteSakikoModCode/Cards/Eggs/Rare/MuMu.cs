using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Rare;

public class MuMu() : CuteSakikoModEggCard(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DamageVar(6m, ValueProp.Move); }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<DefensiveSlash>(IsUpgraded);
            yield return HoverTipFactory.FromCard<FlySlash>(IsUpgraded);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 基础伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 根据概率选择生成的卡牌
        var isDefensive = Owner.RunState.Rng.UpFront.NextDouble() < (IsUpgraded ? 0.1 : 0.25);
        if (isDefensive)
        {
            // 生成防御斩
            var token = CombatState.CreateCard<DefensiveSlash>(Owner);
            if (IsUpgraded && token.IsUpgradable)
                CardCmd.Upgrade(token);
            // 将防御斩加入手牌（但会立即执行效果并移除）
            await CardPileCmd.AddGeneratedCardToCombat(token, PileType.Hand, Owner);
            // 立即执行防御斩的效果
            if (token is DefensiveSlash defSlash)
                await defSlash.ExecuteEffect(choiceContext);
        }
        else
        {
            // 生成飞身跃入斩
            var token = CombatState.CreateCard<FlySlash>(Owner);
            if (IsUpgraded && token.IsUpgradable)
                CardCmd.Upgrade(token);
            await CardPileCmd.AddGeneratedCardToCombat(token, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}