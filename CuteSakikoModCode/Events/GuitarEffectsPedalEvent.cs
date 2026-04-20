using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Enchantments;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Events
{
    public sealed class GuitarEffectsPedalEvent : CustomEventModel
    {
        private const int LowCost = 50;
        private const int HighCost = 100;

        private IHoverTip _enchantTip;

        public override string? CustomInitialPortraitPath =>
            "CuteSakikoMod/images/events/guitar_effects_pedal.png";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new GoldVar(LowCost),
            new DynamicVar("HighCost", HighCost),
        ];

        public override bool IsAllowed(IRunState runState)
        {
            return runState.Players.Any(p => p.Relics.OfType<AnonGuitar>().Any());
        }

        protected override IReadOnlyList<EventOption> GenerateInitialOptions()
        {
            var options = new List<EventOption>();

            var enchantTips = HoverTipFactory.FromEnchantment<PlayEnchantment>();
            _enchantTip = enchantTips.FirstOrDefault();

            if (Owner!.Gold >= LowCost)
                options.Add(Option(BuyLow, tips: _enchantTip));
            if (Owner!.Gold >= HighCost)
                options.Add(Option(BuyHigh, tips: _enchantTip));
            options.Add(Option(NoMoney));

            return options;
        }

        private async Task BuyLow()
        {
            await PlayerCmd.LoseGold(LowCost, Owner!, GoldLossType.Spent);
            await EnchantCards(1);
            SetEventFinished(PageDescription("LOW_SUCCESS"));
        }

        private async Task BuyHigh()
        {
            await PlayerCmd.LoseGold(HighCost, Owner!, GoldLossType.Spent);
            await EnchantCards(3);
            SetEventFinished(PageDescription("HIGH_SUCCESS"));
        }

        private async Task NoMoney()
        {
            var rng = Owner!.RunState.Rng.UpFront;
            if (rng.NextInt(100) < 5)
            {
                SetEventState(PageDescription("SOYO_PAYS"), [
                    Option(GetFreeEnchantAndGold, "SOYO_PAYS", tips: _enchantTip),
                    Option(Refund, "SOYO_PAYS"),
                ]);
            }
            else
            {
                SetEventFinished(PageDescription("NO_MONEY_NOTHING"));
            }
        }

        private async Task GetFreeEnchantAndGold()
        {
            await EnchantCards(3);
            await PlayerCmd.GainGold(50, Owner!);
            SetEventFinished(PageDescription("SOYO_GIFT"));
        }

        private async Task Refund()
        {
            await PlayerCmd.GainGold(150, Owner!);
            SetEventFinished(PageDescription("SOYO_REFUND"));
        }

        private async Task EnchantCards(int count)
        {
            var canonicalEnchantment = ModelDb.Enchantment<PlayEnchantment>();
            var prefs = new CardSelectorPrefs(PageDescription("SELECT_CARD_TITLE"), count, count)
            {
                RequireManualConfirmation = count > 1
            };

            bool Filter(CardModel card) => card.Enchantment == null || card.Enchantment.Id != canonicalEnchantment.Id;

            var selected = await CardSelectCmd.FromDeckForEnchantment(
                Owner!,
                canonicalEnchantment,
                count,
                Filter,
                prefs
            );

            foreach (var card in selected)
            {
                var mutableEnchantment = canonicalEnchantment.ToMutable();
                CardCmd.Enchant(mutableEnchantment, card, 1);
            }
        }
    }
}