using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

[RegisterSharedEvent]
public sealed class StarAnonEvent : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://CuteSakikoMod/images/events/star_anon.png"
    );
    
    public override bool IsShared => true;
    
    public override bool IsAllowed(IRunState runState)
    {
        return ModConfig.EnableModMonsters;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var relicTips = HoverTipFactory.FromRelic<TimeWatch>().ToArray();
        return new List<EventOption>
        {
            new(this, PickUpWatch, InitialOptionKey("PICK_UP"), relicTips),
            new(this, KickAway, InitialOptionKey("KICK_AWAY"))
        };
    }

    private async Task PickUpWatch()
    {
        await RelicCmd.Obtain(ModelDb.Relic<TimeWatch>().ToMutable(), Owner!);
        SetEventFinished(PageDescription("PICK_UP_SUCCESS"));
    }

    private async Task KickAway()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var cardsToRemove = await CardSelectCmd.FromDeckForRemoval(Owner!, prefs);
        if (cardsToRemove.Any())
        {
            var list = cardsToRemove.ToList();
            await CardPileCmd.RemoveFromDeck(list, false);
        }

        SetEventFinished(PageDescription("KICK_AWAY_FAIL"));
    }
}