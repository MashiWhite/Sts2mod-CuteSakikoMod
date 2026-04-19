using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools.Anon;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon
{
    [Pool(typeof(CuteAnonCardPool))]
    public abstract class CuteAnonCard : CustomCardModel
    {
        protected CuteAnonCard(int cost, CardType type, CardRarity rarity, TargetType target)
            : base(cost, type, rarity, target)
        {
        }

        public override string CustomPortraitPath =>
            (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

        public override string PortraitPath =>
            (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

        public override string BetaPortraitPath =>
            (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

        /// <summary>
        /// 触发角色对话（联机同步版本）
        /// </summary>
        protected void TriggerBanter()
        {
            if (Owner?.Creature?.CombatState == null) return;

            string actionType = Type switch
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
}