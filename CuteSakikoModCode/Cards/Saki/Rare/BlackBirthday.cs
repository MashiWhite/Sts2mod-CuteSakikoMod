using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public sealed class BlackBirthday() : CuteSakikoModCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 基础值 = 1（给予1层 BlackRebirthPower），升级后变为2
            yield return new PowerVar<BlackRebirthPower>(2);
        }
    }


    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<BlackRebirthPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BlackRebirthPower>(choiceContext, Owner.Creature, DynamicVars["BlackRebirthPower"].IntValue, Owner.Creature, this);

        var creature = Owner.Creature;
        var currentHp = creature.CurrentHp;
        if (currentHp <= 20) return;
        var lostHp = currentHp - 20;

        await CreatureCmd.SetCurrentHp(creature, 20);
        await CreatureCmd.GainBlock(creature, lostHp, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        // 升级：增加 PowerVar 的值（2 -> 3）
        DynamicVars["BlackRebirthPower"].UpgradeValueBy(1);
    }
}