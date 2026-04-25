
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;

public sealed class KabutoNote : CuteSakikoModRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memorysaki);
        }
    }

    // 在回合开始时触发（每回合都会调用）
    public override async Task AfterSideTurnStart(CombatSide side,ICombatState combatState)
    {
        // 只处理拥有者所在的一侧，且仅在战斗的第一回合（RoundNumber == 1）
        if (side == Owner.Creature.Side && combatState.RoundNumber == 1)
        {
            // 给遗物持有者施加 3 层压力
            await PowerCmd.Apply<PressurePower>(new ThrowingPlayerChoiceContext(),Owner.Creature, 3, Owner.Creature, null);
            // 闪烁遗物图标，提示生效
            Flash();
        }
    }
    
}