
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.RunData;


namespace CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;

[RegisterCharacterStarterRelic(typeof(CuteRana), Order = 0)]
[RegisterTouchOfOrobasRefinement(typeof(BigMatchaParfait))]
public class MatchaParfait : CuteRanaRelic
{
    private static PlayerRunSavedData<PlayerParfaitData> ParfaitChargesSlot => Entry.ParfaitChargesSlot;

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

    public int Charges
    {
        get
        {
            if (Owner?.RunState == null) return 6;
            var data = ParfaitChargesSlot.Get(Owner);
            return data?.Charges ?? 6;
        }
        set
        {
            if (Owner?.RunState == null) return;
            // 使用 Modify 方法安全地修改数据
            ParfaitChargesSlot.Modify(Owner, data =>
            {
                if (data.Charges != value)
                {
                    data.Charges = value;
                    // 注意：Modify 内部会自动标记为脏并保存，但 UI 刷新需要手动调用
                    InvokeDisplayAmountChanged();
                }
            });
        }
    }

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => true;
    public override int DisplayAmount => Charges;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(DrawAmount),
        new EnergyVar(EnergyGain)
    };

    // 右键交互（保持不变）
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

    private static async Task PerformRightClickEffect(PlayerChoiceContext choiceContext, MatchaParfait relic,
        Player player)
    {
        await CardPileCmd.Draw(choiceContext, relic.DrawAmount, player);
        await PlayerCmd.GainEnergy(relic.EnergyGain, player);
        relic.Charges--;
        if (relic.Charges <= 0) relic.Charges = 0;
    }

    // 休息处恢复计数
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is RestSiteRoom) Charges += 5;
        return Task.CompletedTask;
    }

    // 扩展接口
    public static void AddCharges(MatchaParfait relic, int amount) => relic.Charges += amount;
    public static void RemoveCharges(MatchaParfait relic, int amount)
    {
        if (relic == null) return;
        if (relic.Owner.Creature.HasPower<ParfaitTreatPower>())
            return;
        relic.Charges = Math.Max(0, relic.Charges - amount);
    }
    public static void SetDrawAmount(MatchaParfait relic, int amount) => relic.DrawAmount = amount;
    public static void SetEnergyGain(MatchaParfait relic, int amount) => relic.EnergyGain = amount;
}