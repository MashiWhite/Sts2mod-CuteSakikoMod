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

    // 每场战斗开始时，减少1点最大生命值
    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();
        if (Owner == null) return;
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            1m,
            false
        );
    }

    // 每回合开始时获得1点能量，抽1张牌
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;

        // ✅ 修正：GainEnergy 只需要 amount 和 player，不需要 choiceContext
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, player);

        // 抽1张牌（Draw 需要 choiceContext）
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, player);
    }
}