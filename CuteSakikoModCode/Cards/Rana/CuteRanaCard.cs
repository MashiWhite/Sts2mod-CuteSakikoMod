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
                    // 如果有"有人请客"能力，无条件允许打出
                    if (Owner.Creature.HasPower<ParfaitTreatPower>())
                        return true;

                    // 否则检查计数是否足够
                    if (parfait.Charges < eater.GetParfaitConsumeCount())
                        return false;
                }
            }
            return true;
        }
    }

    public interface IEatParfaitCard
    {
        int GetParfaitConsumeCount();
    }
}