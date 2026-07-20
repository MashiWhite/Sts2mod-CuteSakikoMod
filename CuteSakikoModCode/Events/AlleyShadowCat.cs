using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

[RegisterSharedEvent]
public sealed class AlleyShadowCat : CuteSakikoEvent 
{
    private IHoverTip[]? _relicHoverTips;  // 缓存遗物提示

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://CuteSakikoMod/images/events/alley_shadow_cat.png"
    );
    
    public override bool IsShared => false;

    protected override bool IsAllowedInternal(IRunState runState) => true;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // 初始化遗物悬停提示
        _relicHoverTips ??= HoverTipFactory.FromRelic<BlackCatEyes>().ToArray();

        return new List<EventOption>
        {
            // 为靠近选项添加遗物提示
            new(this, ApproachCat, InitialOptionKey("APPROACH"), _relicHoverTips),
            new(this, Leave, InitialOptionKey("LEAVE"))
        };
    }

    private async Task ApproachCat()
    {
        var relic = ModelDb.Relic<BlackCatEyes>().ToMutable();
        await RelicCmd.Obtain(relic, Owner!);
        SetEventFinished(PageDescription("APPROACH_SUCCESS"));
    }

    private async Task Leave()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            8m,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null
        );

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
        var selected = await CardSelectCmd.FromDeckForUpgrade(Owner, prefs);
        if (selected.Any())
        {
            var card = selected.First();
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
        }

        SetEventFinished(PageDescription("LEAVE_SUCCESS"));
    }
}