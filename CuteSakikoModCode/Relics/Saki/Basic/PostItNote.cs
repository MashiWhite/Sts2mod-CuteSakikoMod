
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;

public sealed class PostItNote : CuteSakikoModRelic
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

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        // 只处理拥有者所在的一侧
        if (side != Owner.Creature.Side) return;

        // 第一回合：给自己施加 5 层压力
        if (combatState.RoundNumber == 1)
        {
            await PowerCmd.Apply<PressurePower>(new ThrowingPlayerChoiceContext(),Owner.Creature, 5, Owner.Creature, null);
            Flash(); // 闪烁遗物图标，提示生效
        }

        // 每回合给所有敌人施加 5 层压力
        if (combatState.HittableEnemies != null)
            foreach (var enemy in combatState.HittableEnemies)
                await PowerCmd.Apply<PressurePower>(new ThrowingPlayerChoiceContext(),enemy, 3, Owner.Creature, null);
    }
}