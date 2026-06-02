using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools.Rana;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana;

[RegisterCard(typeof(CuteRanaCardPool), Inherit = true)]
public abstract class CuteRanaCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : ModCardTemplate(cost, type, rarity, target)
{
    public override CardAssetProfile AssetProfile => this.CardAssetProfile();

    protected override bool IsPlayable
    {
        get
        {
            if (this is IEatParfaitCard eater)
            {
                var parfait = Owner?.Relics.OfType<MatchaParfait>().FirstOrDefault();
                if (parfait != null)
                {
                    // 如果有人请客，无条件可打出
                    if (Owner.Creature.HasPower<ParfaitTreatPower>())
                        return true;

                    // 特殊处理：消耗所有杯数（ConsumeAll = true）
                    if (eater.ConsumeAll)
                        return parfait.Charges > 0;

                    // 否则检查杯数是否足够
                    if (parfait.Charges < eater.GetParfaitConsumeCount())
                        return false;
                }
            }
            return true;
        }
    }

    public interface IEatParfaitCard
    {
        /// <summary>
        /// 需要消耗的芭菲杯数（仅在 ConsumeAll = false 时有效）
        /// </summary>
        int GetParfaitConsumeCount();

        /// <summary>
        /// 是否消耗所有杯数（优先级高于 GetParfaitConsumeCount）
        /// </summary>
        bool ConsumeAll => false; // 默认 false，只有消耗所有的卡牌才需要重写为 true
    }
}