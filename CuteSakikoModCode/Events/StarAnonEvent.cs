using CuteSakikoMod.CuteSakikoModCode.Others.Config;
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
public sealed class StarAnonEvent : CuteSakikoEvent
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://CuteSakikoMod/images/events/star_anon.png"
    );

    public override bool IsShared => true;

    /// <summary>无角色门槛，归“普通事件”。</summary>
    protected override bool NeedsModCharacter => false;

    protected override bool IsAllowedInternal(IRunState runState)
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
        // ⚠️ 兼容 DirectConnectIP 离线托管（2026-11-08 修多人卡死）：
        //    托管玩家的远程选择固定返回 Index 类型，走 FromDeckForRemoval 会在
        //    CardSelectCmd 内部 AsDeckCards() 抛异常并卡死事件。对托管玩家跳过选牌。
        //    未装该 mod 时 IsTakenOverByDirectConnect 恒 false → 行为完全不变。
        if (!IsTakenOverByDirectConnect(Owner))
        {
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
            var cardsToRemove = await CardSelectCmd.FromDeckForRemoval(Owner!, prefs);
            if (cardsToRemove.Any())
            {
                var list = cardsToRemove.ToList();
                await CardPileCmd.RemoveFromDeck(list, false);
            }
        }

        SetEventFinished(PageDescription("KICK_AWAY_FAIL"));
    }
}