using System.Threading;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public class GradeFirstNotebook : CuteSakikoEventRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;

#if STS2_AT_LEAST_0_113_0
        // 0.113 起原版 SmithRestSiteOption 变成 sealed 且删掉 SmithCount（固定只升级 1 张），
        // 不能再改次数，改为把原版锻造选项替换成本遗物专用的「锻造两张」选项。
        var vanillaSmith = options.OfType<SmithRestSiteOption>().FirstOrDefault();
        if (vanillaSmith == null) return false;

        int upgradableCount = player.Deck.UpgradableCardCount;
        if (upgradableCount < 2) return false;

        options.Remove(vanillaSmith);
        options.Add(new GradeFirstNotebookSmithOption(player));
        return true;
#else
        var smith = options.OfType<SmithRestSiteOption>().FirstOrDefault();
        if (smith == null) return false;

        // 获取当前牌库中可升级的牌数
        int upgradableCount = player.Deck.UpgradableCardCount;

        // 如果可升级的牌数 <= 当前的敲牌次数，说明没有多余的牌可以敲了
        // 此时直接返回 false，避免原版选牌界面因为要求选 2 张但实际只有 1 张而卡死
        if (upgradableCount <= smith.SmithCount) return false;

        // 增加敲牌次数，上限为当前实际可升级的牌数
        smith.SmithCount = Math.Min(smith.SmithCount + 1, upgradableCount);
        return true;
#endif
    }
}

#if STS2_AT_LEAST_0_113_0
/// <summary>
///     0.113 专用的锻造选项：与「年级第一的笔记」联动，一次升级两张牌。
///     OptionId 沿用原版 "SMITH"，因此复用原版标题/图标/音效，只替换描述与升级张数。
/// </summary>
internal sealed class GradeFirstNotebookSmithOption : RestSiteOption
{
    private IEnumerable<CardModel>? _selection;

    public GradeFirstNotebookSmithOption(Player owner) : base(owner)
    {
    }

    public override string OptionId => "SMITH";

    public override LocString Description => new("rest_site_ui", "CUTE_SAKIKO_MOD_OPTION_SMITH_EXTRA_DESC");

    public override bool IsEnabled => Owner.Deck.UpgradableCardCount != 0;

    public override async Task<bool> OnSelect()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 2)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        _selection = await CardSelectCmd.FromDeckForUpgrade(Owner, prefs);
        if (!_selection.Any())
        {
            return false;
        }

        foreach (var card in _selection)
        {
            CardCmd.Upgrade(card, CardPreviewStyle.None);
        }

        await Hook.AfterRestSiteSmith(Owner.RunState, Owner);
        return true;
    }

    public override async Task DoLocalPostSelectVfx(CancellationToken ct = default)
    {
        NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(NCardSmithVfx.Create(_selection!.ToArray()));
        await Cmd.CustomScaledWait(1f, 2f, ignoreCombatEnd: false, ct);
    }
}
#endif
