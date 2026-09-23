using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

public class PressureIntoBlade() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    // 出鞘关键词 + 消耗
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CutesakiKeywords.Sword.GetModCardKeyword(), CardKeyword.Exhaust];

    // 本次操作中，要附加到哪把剑、附加多少
    private KnightSword? _bonusTarget;
    private int _bonusAmount;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<KnightSword>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sword.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 当前压力层数
        var pressure = Owner.Creature.GetPower<PressurePower>();
        int pressureAmount = pressure?.Amount ?? 0;

        // 2. 手牌中的骑士之剑（出鞘逻辑已在 BeforeCardPlayed 保证其存在）
        var hand = PileType.Hand.GetPile(Owner);
        var sword = hand?.Cards.OfType<KnightSword>().FirstOrDefault();
        if (sword == null) return;

        // 3. 消耗所有压力
        if (pressure != null && pressureAmount > 0)
            await PowerCmd.ModifyAmount(choiceContext, pressure, -pressureAmount, Owner.Creature, this);

        // 4. 记录本次附加伤害，交由 ModifyDamageAdditive 生效
        if (pressureAmount > 0)
        {
            _bonusTarget = sword;
            _bonusAmount = pressureAmount;
        }

        // 5. 打出骑士之剑（这一次攻击会通过 ModifyDamageAdditive 吃到附加伤害）
        await CardCmd.AutoPlay(choiceContext, sword, cardPlay.Target);

        // 6. 清理，保证只影响本次攻击
        _bonusTarget = null;
        _bonusAmount = 0;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        // 只对本次指定的那把剑生效
        if (_bonusAmount > 0 && cardSource == _bonusTarget)
            return _bonusAmount;
        return 0m;
    }

    protected override void OnUpgrade()
    {
        // 升级后移除消耗
        RemoveKeyword(CardKeyword.Exhaust);
    }
}