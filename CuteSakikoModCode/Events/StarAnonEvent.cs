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

    // 可选：仍然需要防止在地图生成时出现多次补丁控制？但补丁已经强制前三次，IsAllowed 可以始终返回 true
    // 如果你想额外限制整个 run 只出现一次（与补丁计数器重复），可以保留一个标志，但不是必须。
    // 为简单起见，IsAllowed 直接返回 true，让补丁完全控制出现次数。
    public override bool IsAllowed(IRunState runState)
    {
        return true;
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