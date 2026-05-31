using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;

[RegisterCharacterStarterRelic(typeof(CuteRana), Order = 0)]
[RegisterTouchOfOrobasRefinement(typeof(BigMatchaParfait))]
public class MatchaParfait : CuteRanaRelic
{
    [SavedProperty]
    private int _charges { get; set; } = 6;

    private int _drawAmount = 1;
    private int _energyGain = 1;

    public int DrawAmount { get => _drawAmount; set { _drawAmount = value; if (DynamicVars.TryGetValue("Cards", out var dv)) dv.BaseValue = value; } }
    public int EnergyGain { get => _energyGain; set { _energyGain = value; if (DynamicVars.TryGetValue("Energy", out var dv)) dv.BaseValue = value; } }

    public int Charges { get => _charges; set { if (_charges == value) return; _charges = value; InvokeDisplayAmountChanged(); } }

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShowCounter => true;
    public override int DisplayAmount => Charges;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(DrawAmount), new EnergyVar(EnergyGain) };

    public async Task ExecuteRightClickAsync(Player player)
    {
        try
        {
            Entry.Logger.Info("[芭菲] 效果开始");
            var ctx = new TrivialPlayerChoiceContext();
            await CardPileCmd.Draw(ctx, DrawAmount, player);
            Entry.Logger.Info("[芭菲] 抽牌完成");
            await PlayerCmd.GainEnergy(EnergyGain, player);
            Entry.Logger.Info("[芭菲] 加能量完成");
            Charges--;
            if (Charges < 0) Charges = 0;
            Entry.Logger.Info("[芭菲] 效果完成");
        }
        catch (Exception ex)
        {
            Entry.Logger.Error($"[芭菲] 效果异常: {ex}");
        }
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is RestSiteRoom) Charges += 5;
        return Task.CompletedTask;
    }

    public static void AddCharges(MatchaParfait relic, int amount) => relic.Charges += amount;
    public static void RemoveCharges(MatchaParfait relic, int amount)
    {
        if (relic == null) return;
        if (relic.Owner.Creature.HasPower<ParfaitTreatPower>()) return;
        relic.Charges = Math.Max(0, relic.Charges - amount);
    }
    public static void SetDrawAmount(MatchaParfait relic, int amount) => relic.DrawAmount = amount;
    public static void SetEnergyGain(MatchaParfait relic, int amount) => relic.EnergyGain = amount;

    private sealed class TrivialPlayerChoiceContext : PlayerChoiceContext
    {
        public override Task SignalPlayerChoiceBegun(PlayerChoiceOptions options) => Task.CompletedTask;
        public override Task SignalPlayerChoiceEnded() => Task.CompletedTask;
    }
}