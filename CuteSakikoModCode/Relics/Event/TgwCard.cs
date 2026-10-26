using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Nodes.SakiButton;
using CuteSakikoMod.CuteSakikoModCode.Others.Telemetry;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interactions.RightClick;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public class TgwCard : CuteSakikoEventRelic, IModRightClickableRelic
{
    /// <summary>利息倍率：应还 = 本金 × 1.1。</summary>
    public const decimal InterestMultiplier = 1.1m;

    /// <summary>高消费限制触发的战斗场次。</summary>
    public const int ShopLimitCombatThreshold = 5;

    private int _debt;
    private int _level;
    private int _consecutiveCombats;
    private bool _shopLimitActive;
    private int _lastLoanFloor = -1;
    private bool _isLoaning;
    private bool _isRepaying;

    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool IsAllowedInShops => false;

    // —— 持久化状态（[SavedProperty] 只能标属性）——

    /// <summary>当前应还总额（含利息）。</summary>
    [SavedProperty]
    public int Debt
    {
        get => _debt;
        set
        {
            AssertMutable();
            _debt = Math.Max(0, value);
            DynamicVars["Debt"].BaseValue = _debt;
            DynamicVars["DebtPercent"].BaseValue = _debt / 5;
            UpdateStatus();
            InvokeDisplayAmountChanged();
        }
    }

    /// <summary>会员卡等级（0~3）。</summary>
    [SavedProperty]
    public int Level
    {
        get => _level;
        set
        {
            AssertMutable();
            _level = Math.Clamp(value, 0, 3);
            DynamicVars["Level"].BaseValue = _level;
            DynamicVars["LoanAmount"].BaseValue = LoanPrincipal;
            RelicIconChanged();
            RefreshInventoryIcon();
            InvokeDisplayAmountChanged();
        }
    }

    /// <summary>带欠款连续战斗的场次。</summary>
    [SavedProperty]
    public int ConsecutiveCombats
    {
        get => _consecutiveCombats;
        set
        {
            AssertMutable();
            _consecutiveCombats = Math.Max(0, value);
        }
    }

    /// <summary>是否已触发高消费限制。</summary>
    [SavedProperty]
    public bool ShopLimitActive
    {
        get => _shopLimitActive;
        set
        {
            AssertMutable();
            _shopLimitActive = value;
        }
    }

    /// <summary>上次贷款发生的楼层。同一层（同一商店）只能贷一次。</summary>
    [SavedProperty]
    public int LastLoanFloor
    {
        get => _lastLoanFloor;
        set
        {
            AssertMutable();
            _lastLoanFloor = value;
        }
    }

    // —— 运行时状态（不持久化）——

    /// <summary>本次商店访问累计消费，进入商店时重置。</summary>
    public int ShopSpentThisVisit { get; set; }

    public void SetLoaning(bool value) => _isLoaning = value;

    // —— 数值 ——

    /// <summary>本次可贷到的本金。</summary>
    public int LoanPrincipal => 50 + 25 * Level;

    /// <summary>本次贷款需要偿还的总额（含 10% 利息，向上取整）。</summary>
    public int LoanDebt => (int)Math.Ceiling(LoanPrincipal * InterestMultiplier);

    /// <summary>剩余多少场战斗触发高消费限制。</summary>
    public int RemainingCombats => Math.Max(0, ShopLimitCombatThreshold - ConsecutiveCombats);

    // —— 动态贴图 ——

    public override string? CustomIconPath =>
        $"res://{Entry.ModId}/images/relics/TgwCard{Level}.png";

    public override string? CustomIconOutlinePath =>
        $"res://{Entry.ModId}/images/relics/TgwCard{Level}.png";

    public override string? CustomBigIconPath =>
        $"res://{Entry.ModId}/images/relics/big/TgwCard{Level}.png";

    // —— 动态描述变量 ——

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("Debt", 0);
            yield return new DynamicVar("Level", 0);
            yield return new DynamicVar("LoanAmount", 50);
            yield return new DynamicVar("DebtPercent", 0);
        }
    }

    // —— 生命周期 ——

    public override Task AfterObtained()
    {
        DynamicVars["Debt"].BaseValue = Debt;
        DynamicVars["Level"].BaseValue = Level;
        DynamicVars["LoanAmount"].BaseValue = LoanPrincipal;
        DynamicVars["DebtPercent"].BaseValue = Debt / 5;
        UpdateStatus();
        return Task.CompletedTask;
    }

    private void UpdateStatus()
    {
        Status = Debt > 0 ? RelicStatus.Active : RelicStatus.Normal;
    }

    // —— 遗物栏小图刷新 ——

    private void RefreshInventoryIcon()
    {
        var inventory = NRun.Instance?.GlobalUi?.RelicInventory;
        if (inventory == null) return;

        foreach (var holder in inventory.RelicNodes)
        {
            var nRelic = holder.Relic;
            if (nRelic == null) continue;

            RelicModel model;
            try { model = nRelic.Model; }
            catch (InvalidOperationException) { continue; }

            if (!ReferenceEquals(model, this)) continue;

            nRelic.Model = model;
            break;
        }
    }

    // —— 额外悬浮提示 ——

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (Debt > 0)
            {
                var title = new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_TGW_CARD.high_spend_title");
                var desc = ShopLimitActive
                    ? new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_TGW_CARD.high_spend_active")
                    : new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_TGW_CARD.high_spend_countdown");

                if (!ShopLimitActive)
                    desc.Add("RemainingCombats", RemainingCombats);

                yield return new HoverTip(title, desc);
            }

            var emptyTitle = new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_TGW_CARD.empty_title");
            var info = new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_TGW_CARD.debt_info");
            info.Add("Debt", Debt);
            info.Add("LevelName", GetLevelNameText());
            info.Add("LoanAmount", LoanPrincipal);
            info.Add("LoanDebt", LoanDebt);
            yield return new HoverTip(emptyTitle, info);
        }
    }

    private string GetLevelNameText()
    {
        var loc = new LocString("relics", $"CUTE_SAKIKO_MOD_RELIC_TGW_CARD.level_{Level}");
        return loc.GetFormattedText();
    }

    // —— 金币收益惩罚 ——

    public override decimal ModifyGoldGained(Player player, decimal amount)
    {
        if (player != Owner) return amount;
        if (_isLoaning) return amount;
        if (Debt <= 0) return amount;
        return amount * (1 - Debt / 500m);
    }

    // —— 右键结清 ——

    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        var owner = Owner;
        if (owner == null) return false;
        // 这里保留本地判断是没问题的，确保只有发起者自己的 UI 能够触发右键
        if (!LocalContext.IsMe(owner)) return false;
        return Debt > 0 && owner.Gold > 0;
    }

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (_isRepaying) return;
        _isRepaying = true;
        try
        {
            var owner = Owner;
            if (owner == null) return;
            
            // ★ 关键修改：移除 !LocalContext.IsMe(owner) 拦截，让所有端同步执行状态变更
            
            if (Debt <= 0 || owner.Gold <= 0) return;

            int pay = Math.Min(owner.Gold, Debt);
            if (pay <= 0) return;

            int debtBefore = Debt;
            int levelBefore = Level;

            // 两端各自扣除本地的金币状态，因为 PlayerCmd.LoseGold 只是纯本地计算修改，不会发包
            await PlayerCmd.LoseGold(pay, owner, GoldLossType.Spent);
            
            // 两端各自扣减遗物属性
            Debt -= pay;

            bool fullyRepaid = Debt == 0;
            if (fullyRepaid)
            {
                if (Level < 3) Level++;
                ConsecutiveCombats = 0;
                ShopLimitActive = false;
            }

            // 仅本地端负责反馈特效和上传遥测
            if (LocalContext.IsMe(owner))
            {
                Flash();

                try
                {
                    CuteSakikoModTelemetry.CaptureTgwRepaid(
                        owner.Character.Id.Entry,
                        CuteSakikoModTelemetry.GetCurrentFloor(owner),
                        pay, debtBefore, Debt,
                        fullyRepaid, levelBefore, Level);
                }
                catch (Exception ex)
                {
                    Entry.Logger.Warn($"[TgwCard] Telemetry failed: {ex.Message}");
                }
            }
        }
        finally
        {
            _isRepaying = false;
            NotifyLoanButton();
        }
    }

    /// <summary>通知商店里的贷款按钮刷新可点击状态（不在商店时为 null 直接跳过）。</summary>
    private void NotifyLoanButton()
    {
        var room = NMerchantRoom.Instance;
        if (room == null) return;
        var button = room.GetNodeOrNull<TogawaLoanButton>("TogawaLoanButton");
        button?.RefreshState();
    }

    // —— 贷款资格 ——

    public bool CanLoan(Player player)
    {
        if (Debt > 0) return false;
        if (LastLoanFloor == player.RunState.TotalFloor) return false;
        return true;
    }

    public void MarkLoaned(Player player)
    {
        LastLoanFloor = player.RunState.TotalFloor;
    }

    // —— 连续战斗计数 ——

    public override Task AfterCombatVictory(CombatRoom room)
    {
        if (Debt > 0)
        {
            ConsecutiveCombats++;
            if (ConsecutiveCombats >= ShopLimitCombatThreshold && !ShopLimitActive)
            {
                ShopLimitActive = true;
                
                if (LocalContext.IsMe(Owner))
                {
                    Flash();
                }
              
                var player = Owner;
                if (player != null && LocalContext.IsMe(player))
                {
                    CuteSakikoModTelemetry.CaptureTgwShopLimit(
                        player.Character.Id.Entry,
                        CuteSakikoModTelemetry.GetCurrentFloor(player),
                        Debt,
                        ConsecutiveCombats);
                }
     
            }
        }
        return Task.CompletedTask;
    }

    // —— 商店累计消费跟踪 ——

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is MerchantRoom)
            ShopSpentThisVisit = 0;
        return Task.CompletedTask;
    }

    public override Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        if (player == Owner)
            ShopSpentThisVisit += goldSpent;
        return Task.CompletedTask;
    }
}