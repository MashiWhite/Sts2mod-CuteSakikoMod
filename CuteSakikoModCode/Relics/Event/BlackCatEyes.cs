using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public class BlackCatEyes : CuteSakikoEventRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new EnergyVar(1);
            yield return new CardsVar(1);
        }
    }

    // 每场战斗开始时，减少3点最大生命值
    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();
        if (Owner == null) return;

        var creature = Owner.Creature;
        var maxHp = creature.MaxHp;

        // 如果当前最大生命值已经<=1，不再扣除
        if (maxHp <= 1) return;

        // 计算实际扣除量：最多扣除3点，但确保剩余至少1点
        var loss = Math.Min(3m, maxHp - 1);

        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            creature,
            loss,
            false // 不是来自卡牌
        );
    }

    // 每回合开始时获得1点能量，抽1张牌
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, player);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, player);
    }
}