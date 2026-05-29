using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;

[RegisterCharacterStarterRelic(typeof(CuteRana),Order = 0)]
[RegisterTouchOfOrobasRefinement(typeof(BigMatchaParfait))]
public class MatchaParfait : CuteRanaRelic
{
    // -------- 可扩展属性（供其他卡牌/遗物修改）--------
    private int _drawAmount = 1;
    private int _energyGain = 1;

    public int DrawAmount
    {
        get => _drawAmount;
        set
        {
            _drawAmount = value;
            if (DynamicVars.TryGetValue("Cards", out var dv)) dv.BaseValue = value;
        }
    }

    public int EnergyGain
    {
        get => _energyGain;
        set
        {
            _energyGain = value;
            if (DynamicVars.TryGetValue("Energy", out var dv)) dv.BaseValue = value;
        }
    }

    // -------- 核心计数器（自动存档，手动属性以刷新UI）--------
    private int _charges = 6;

    [SavedProperty]
    public int Charges
    {
        get => _charges;
        set
        {
            if (_charges == value) return;
            _charges = value;
            InvokeDisplayAmountChanged(); // 刷新遗物图标上的计数器
        }
    }

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => true;
    public override int DisplayAmount => Charges;

    // 动态变量：使用默认名称 "Cards" 和 "Energy"
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(DrawAmount),
        new EnergyVar(EnergyGain),
    };

    // ========== 右键处理 ==========
    public static bool CanRightClick(ModRightClickContext ctx)
    {
        var relic = ctx.Model as MatchaParfait;
        var player = ctx.Player;
        if (relic == null || player == null || relic.Charges <= 0) return false;
        var combatState = player.Creature?.CombatState;
        return combatState != null && combatState.CurrentSide == CombatSide.Player;
    }

    public static async Task OnRightClick(ModRightClickExecutionContext ctx)
    {
        var relic = ctx.Model as MatchaParfait;
        var player = ctx.Player;
        if (relic == null || player == null) return;
        var combatState = player.Creature?.CombatState;
        if (combatState == null || combatState.CurrentSide != CombatSide.Player) return;

        relic.Flash();
        var hookCtx = new HookPlayerChoiceContext(player, player.NetId, GameActionType.Combat);
        var effectTask = PerformRightClickEffect(hookCtx, relic, player);
        await hookCtx.AssignTaskAndWaitForPauseOrCompletion(effectTask);
    }

    private static async Task PerformRightClickEffect(PlayerChoiceContext choiceContext, MatchaParfait relic, Player player)
    {
        await CardPileCmd.Draw(choiceContext, relic.DrawAmount, player);
        await PlayerCmd.GainEnergy(relic.EnergyGain, player);
        relic.Charges--; // 通过属性 setter 触发 InvokeDisplayAmountChanged
        if (relic.Charges <= 0) relic.Charges = 0;
    }

    // ========== 休息处恢复计数 ==========
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is RestSiteRoom) Charges += 5; // 属性赋值，自动刷新UI
        return Task.CompletedTask;
    }

    // ========== 扩展接口 ==========
    public static void AddCharges(MatchaParfait relic, int amount) { if (relic != null) relic.Charges += amount; }
    public static void RemoveCharges(MatchaParfait relic, int amount) { if (relic != null) relic.Charges = Math.Max(0, relic.Charges - amount); }
    public static void SetDrawAmount(MatchaParfait relic, int amount) { if (relic != null) relic.DrawAmount = amount; }
    public static void SetEnergyGain(MatchaParfait relic, int amount) { if (relic != null) relic.EnergyGain = amount; }
}