using CuteSakikoMod.CuteSakikoModCode.Enchantments;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

[RegisterSharedEvent]
public sealed class GuitarEffectsPedalEvent : ModEventTemplate
{
    private const int LowCost = 50;
    private const int HighCost = 100;

    private IHoverTip[] _enchantHoverTips;

    public override string? CustomInitialPortraitPath =>
        "CuteSakikoMod/images/events/guitar_effects_pedal.png";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(LowCost),
        new("HighCost", HighCost)
    ];

    public override bool IsAllowed(IRunState runState)
    {
        // 例如：只有拥有 AnonGuitar 遗物的玩家才能遇到
        return runState.Players.Any(p => p.Relics.OfType<AnonGuitar>().Any());
    }

    // 生成初始选项
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        _enchantHoverTips ??= HoverTipFactory.FromEnchantment<PlayEnchantment>().ToArray();

        // 悬停提示（附魔效果）
        var enchantHoverTips = HoverTipFactory.FromEnchantment<PlayEnchantment>().ToArray();

        if (Owner!.Gold >= LowCost)
            options.Add(new EventOption(this, BuyLow, InitialOptionKey("BUY_LOW"), enchantHoverTips));
        if (Owner!.Gold >= HighCost)
            options.Add(new EventOption(this, BuyHigh, InitialOptionKey("BUY_HIGH"), enchantHoverTips));
        options.Add(new EventOption(this, NoMoney, InitialOptionKey("NO_MONEY"))); // 无额外提示

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
            SetEventState(
                PageDescription("SOYO_PAYS"),
                new[]
                {
                    new EventOption(this, GetFreeEnchantAndGold, ModOptionKey("SOYO_PAYS", "GET_FREE_ENCHANT_AND_GOLD"),
                        _enchantHoverTips ?? Array.Empty<IHoverTip>()),
                    new EventOption(this, Refund, ModOptionKey("SOYO_PAYS", "REFUND"))
                }
            );
        else
            SetEventFinished(PageDescription("NO_MONEY_NOTHING"));
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

        // ✅ 修正：只允许没有任何附魔的卡牌
        bool Filter(CardModel card)
        {
            return card.Enchantment == null;
        }

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