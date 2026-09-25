
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public class MaskInSteam : CuteSakikoEventRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DynamicVar("HealPercent", 20m); }
    }

    // 1) 遗物拥有者自己选择 HEAL 时，治疗量额外 +20% 最大生命值
    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
    {
        if (creature != Owner.Creature) return amount;
        return amount + creature.MaxHp * 0.2m;
    }

    // 2) 拥有者完成治疗后，主动给所有其他队友额外治疗 20%
    public override async Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        if (player != Owner) return;
        if (isMimicked) return;

        Flash();
        Status = RelicStatus.Normal;

        // 遍历所有其他玩家，给予 20% 最大生命值的额外治疗
        foreach (var other in Owner.RunState.Players)
        {
            if (other == Owner) continue;
            if (other.Creature.IsDead) continue;

            var healAmount = other.Creature.MaxHp * 0.2m;
            await CreatureCmd.Heal(other.Creature, healAmount);
        }
    }

    // 3) 休息处 UI 的额外治疗提示（可选）
    public override IReadOnlyList<LocString> ModifyExtraRestSiteHealText(
        Player player,
        IReadOnlyList<LocString> currentExtraText)
    {
        // 只在拥有者本地显示
        if (!LocalContext.IsMe(Owner)) return currentExtraText;

        // 只在“拥有者自己选 HEAL”时的描述里显示
        if (player != Owner) return currentExtraText;

        var list = currentExtraText.ToList();
        var extra = AdditionalRestSiteHealText;
        if (extra != null) list.Add(extra);
        return list;
    }

    // 4) 进入休息处时高亮，其他房间恢复
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        Status = room is RestSiteRoom ? RelicStatus.Active : RelicStatus.Normal;
        return Task.CompletedTask;
    }
}