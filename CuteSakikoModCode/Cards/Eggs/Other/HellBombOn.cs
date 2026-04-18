using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

[Pool(typeof(TokenCardPool))]
public class HellBombOn : CustomCardModel
{
    public HellBombOn() : base(1, CardType.Skill, CardRarity.Token, TargetType.AnyAlly)
    {
    }

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    public override bool HasTurnEndInHandEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(75m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust); }
    }

    // 主动打出：传递给目标队友
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        var targetPlayer = cardPlay.Target.Player;
        if (targetPlayer == null) return;

        // 创建新的炸弹给目标（保持升级状态）
        var newBomb = CombatState.CreateCard<HellBombOn>(targetPlayer);
        if (IsUpgraded) CardCmd.Upgrade(newBomb);
        await CardPileCmd.AddGeneratedCardToCombat(newBomb, PileType.Hand, true);

        // 移除当前炸弹
        await CardPileCmd.RemoveFromCombat(this);
    }

    // 回合结束在手牌中：爆炸
    public override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        // 先对敌人造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 再对自己造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(Owner.Creature)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 战斗未结束且卡牌仍在牌堆中才移除
        if (!CombatManager.Instance.IsEnding && Pile != null && Pile.IsCombatPile)
            await CardPileCmd.RemoveFromCombat(this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(25m); // 75 → 100
    }
}