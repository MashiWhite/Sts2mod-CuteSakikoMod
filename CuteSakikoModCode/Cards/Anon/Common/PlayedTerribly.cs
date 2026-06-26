using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class PlayedTerribly() : CuteAnonCard(0, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(5m, ValueProp.Move);
            yield return new RepeatVar(2);
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromCard<LayFlat>(IsUpgraded); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var combat = Owner.Creature.CombatState;
        if (combat == null) return;

        var hitCount = DynamicVars.Repeat.IntValue;
        var damage = DynamicVars.Damage.BaseValue;

        // 一次多段随机攻击
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .TargetingRandomOpponents(combat)
            .WithHitCount(hitCount)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 添加躺平到手牌
        var layFlatCard = CombatState.CreateCard<LayFlat>(Owner);
        if (IsUpgraded)
        {
            layFlatCard.UpgradeInternal();
            layFlatCard.FinalizeUpgradeInternal();
        }
        await CardPileCmd.AddGeneratedCardToCombat(layFlatCard, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(1);
    }
}