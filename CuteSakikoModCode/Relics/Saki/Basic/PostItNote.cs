using BaseLib.Abstracts;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using StringExtensions = BaseLib.Extensions.StringExtensions;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;

[Pool(typeof(CuteSakiRelicPool))]
public sealed class PostItNote : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    // 大图标路径（用于遗物详情等）
    protected override string BigIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").BigRelicImagePath();

    // 普通图标路径（背包、状态栏小图标）
    public override string PackedIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").RelicImagePath();

    protected override string PackedIconOutlinePath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + "_outline.png").RelicImagePath();

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memorysaki);
        }
    }

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        // 只处理拥有者所在的一侧
        if (side != Owner.Creature.Side) return;

        // 第一回合：给自己施加 5 层压力
        if (combatState.RoundNumber == 1)
        {
            await PowerCmd.Apply<PressurePower>(Owner.Creature, 5, Owner.Creature, null);
            Flash(); // 闪烁遗物图标，提示生效
        }

        // 每回合给所有敌人施加 5 层压力
        if (combatState.HittableEnemies != null)
            foreach (var enemy in combatState.HittableEnemies)
                await PowerCmd.Apply<PressurePower>(enemy, 3, Owner.Creature, null);
    }
}