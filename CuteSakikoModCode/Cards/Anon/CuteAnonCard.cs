using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools.Anon;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon;

[RegisterCard(typeof(CuteAnonCardPool), Inherit = true)]
public abstract class CuteAnonCard(int energyCost, CardType type, CardRarity rarity, TargetType targetType)
    : ModCardTemplate(energyCost, type, rarity, targetType)
{
    public override CardAssetProfile AssetProfile => this.CardAssetProfile();


    public virtual string ChordId => null;

    /// <summary>
    ///     触发角色对话（联机同步版本）
    /// </summary>
    protected void TriggerBanter()
    {
        if (Owner?.Creature?.CombatState == null) return;

        var actionType = Type switch
        {
            CardType.Attack => "attack",
            CardType.Skill => "skill",
            CardType.Power => "power",
            _ => null
        };

        if (actionType != null)
        {
            // 使用全局同步的 UpFront 随机种子，保证所有玩家看到同一句话
            var rng = Owner.RunState.Rng.UpFront;
            CharacterBanterHelper.TrySayBanter(Owner.Creature, actionType, rng);
        }
    }
}