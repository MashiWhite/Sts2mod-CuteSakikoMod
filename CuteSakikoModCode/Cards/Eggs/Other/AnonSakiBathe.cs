
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other
{
    [Pool(typeof(TokenCardPool))]
    public class AnonSakiBathe : CustomCardModel
    {
        public AnonSakiBathe() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
        {
        }

        public override string PortraitPath =>
            (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                // 基础版本有消耗
                yield return CardKeyword.Exhaust;
            }
        }

        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new CardsVar(2);
                yield return new EnergyVar(1);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            
            // 清除一个随机Debuff
            var debuffs = Owner.Creature.Powers.Where(p => p.Type == PowerType.Debuff).ToList();
            if (debuffs.Any())
            {
                var toRemove = debuffs[Owner.RunState.Rng.CombatCardSelection.NextInt(debuffs.Count)];
                await PowerCmd.Remove(toRemove);
            }

            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Cards.UpgradeValueBy(1m);   // 2 → 3
            DynamicVars.Energy.UpgradeValueBy(1m);  // 1 → 2
            RemoveKeyword(CardKeyword.Exhaust);     // 移除消耗
        }
    }
}