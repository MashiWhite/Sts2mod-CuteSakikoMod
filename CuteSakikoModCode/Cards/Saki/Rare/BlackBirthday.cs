using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public sealed class BlackBirthday() : CuteSakikoModCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 基础值 = 1（给予1层 BlackRebirthPower），升级后变为2
            yield return new PowerVar<BlackRebirthPower>(1);
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
        await PowerCmd.Apply<BlackRebirthPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);

        var creature = Owner.Creature;
        var currentHp = creature.CurrentHp;
        if (currentHp <= 10) return;

        var lostHp = currentHp - 10;

        await CreatureCmd.SetCurrentHp(creature, 10);

        await DamageCmd.Attack(lostHp)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 从 DynamicVars 读取层数（基础1，升级后2）
        var powerAmount = (int)DynamicVars["BlackRebirthPower"].BaseValue;
        await PowerCmd.Apply<BlackRebirthPower>(choiceContext, creature, powerAmount, creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：增加 PowerVar 的值（1 -> 2）
        DynamicVars["BlackRebirthPower"].UpgradeValueBy(1);
    }
}