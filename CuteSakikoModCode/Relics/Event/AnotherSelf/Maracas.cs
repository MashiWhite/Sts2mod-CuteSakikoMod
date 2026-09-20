using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interactions.RightClick;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event.AnotherSelf;

public class Maracas : CuteSakikoEventRelic, IModRightClickableRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    private bool _usedThisCombat;
    private bool _pendingExtraTurn;

    private bool UsedThisCombat
    {
        get => _usedThisCombat;
        set
        {
            AssertMutable();
            _usedThisCombat = value;
        }
    }

    private bool PendingExtraTurn
    {
        get => _pendingExtraTurn;
        set
        {
            AssertMutable();
            _pendingExtraTurn = value;
        }
    }

    // 进入战斗 / 拾取时状态重置
    public override Task AfterObtained()
    {
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    // 本地预检：未使用过、未激活时才允许右键
    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        return CombatManager.Instance.IsInProgress
            && !UsedThisCombat
            && !PendingExtraTurn
            && Status != RelicStatus.Disabled;
    }

    public Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (!CombatManager.Instance.IsInProgress || UsedThisCombat || PendingExtraTurn)
            return Task.CompletedTask;

        PendingExtraTurn = true;
        Status = RelicStatus.Active; // 已就绪，高亮
        Flash();
        return Task.CompletedTask;
    }

    public override bool ShouldTakeExtraTurn(Player player)
    {
        return PendingExtraTurn && !UsedThisCombat && player == Owner;
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player != Owner)
            return Task.CompletedTask;

        // 关键：如果不是沙锤等待触发的额外回合，忽略
        if (!PendingExtraTurn)
            return Task.CompletedTask;

        UsedThisCombat = true;
        PendingExtraTurn = false;
        Status = RelicStatus.Disabled;
        Flash();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        UsedThisCombat = false;
        PendingExtraTurn = false;
        Status = RelicStatus.Normal; // 战斗结束恢复
        return Task.CompletedTask;
    }
}