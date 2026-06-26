using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Systems;

namespace CuteSakikoMod.CuteSakikoModCode.Potions.Shared.Rare
{
    /// <summary>
    /// 蔬菜汁 - 获得3层虚弱，随机清除1个Debuff
    /// </summary>
    public sealed class VegetableJuice : CuteSakikoSharedPotion
    {
        public override PotionRarity Rarity => PotionRarity.Rare;
        public override PotionUsage Usage => PotionUsage.CombatOnly;
        public override TargetType TargetType => CutesakiTargets.Anyone;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new PowerVar<WeakPower>(3m)
        ];

        protected override IEnumerable<IHoverTip> AdditionalHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<WeakPower>();
            }
        }

        protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
        {
            if (target == null || !target.IsAlive)
                return;

            // 随机清除1个Debuff
            var debuffs = target.Powers.Where(p => p.Type == PowerType.Debuff).ToList();
            if (debuffs.Count > 0)
            {
                var toRemove = debuffs[Owner.RunState.Rng.CombatCardSelection.NextInt(debuffs.Count)];
                await PowerCmd.Remove(toRemove);
            }
            
            // 获得3层虚弱（施加给目标）
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                target,
                DynamicVars["WeakPower"].BaseValue,
                Owner.Creature,
                null
            );
        }
    }
}