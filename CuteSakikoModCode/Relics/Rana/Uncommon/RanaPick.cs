
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Uncommon
{
    public class RanaPick : CuteRanaRelic
    {
        public override RelicRarity Rarity => RelicRarity.Uncommon;

        protected override IEnumerable<DynamicVar> CanonicalVars => [];
        
        protected override IEnumerable<IHoverTip> AdditionalHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<RanaLivePower>();
                yield return HoverTipFactory.FromPower<LiveSweetPower>();
            }
        }
        
        public override bool TryModifyPowerAmountReceived(
            PowerModel canonicalPower,
            Creature target,
            Decimal amount,
            Creature? giver,
            out Decimal modifiedAmount)
        {
            modifiedAmount = amount;

            // 仅当目标为自己、且施加的 Power 是 RanaLivePower 时生效
            if (target != Owner?.Creature) return false;
            if (canonicalPower is not RanaLivePower) return false;

            // 额外 +1 层
            modifiedAmount = amount + 1;
            Flash(); // 遗物闪一下视觉效果
            return true;
        }
    }
}