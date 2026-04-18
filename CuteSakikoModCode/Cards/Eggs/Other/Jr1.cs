using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

[Pool(typeof(TokenCardPool))]
public class Jr1 : CustomCardModel
{
    public Jr1() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<IHoverTip> ExtraHoverTips
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
            yield return new DamageVar(IsUpgraded ? 13m : 10m, ValueProp.Move); // 单次高伤
            yield return new RepeatVar(IsUpgraded ? 2 : 2); // 两次低伤次数
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 两次低伤害（基础8，升级10）
        var lowDamage = IsUpgraded ? 10 : 8;
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
        await CardPileCmd.AddGeneratedCardToCombat(jr2, PileType.Hand, true);

        await CardPileCmd.RemoveFromCombat(this);
    }

    protected override void OnUpgrade()
    {
        // 升级效果在 DynamicVars 和逻辑中处理
    }
}