
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss.TenDay;

[RegisterMonster]
public class TenDayTomori : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 100, 75);
    public override int MaxInitialHp => MinInitialHp;

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/tenday/tendaytomori.tscn"
    );

    private int Amount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
        => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var candidates = new[] { "TEMP_STR_DOWN", "TEMP_DEX_DOWN" };

        var strDown = new RandomMoveState("TEMP_STR_DOWN", TempStrDownMove, candidates, new DebuffIntent());
        var dexDown = new RandomMoveState("TEMP_DEX_DOWN", TempDexDownMove, candidates, new DebuffIntent());

        var states = new List<MonsterState> { strDown, dexDown };
        return new MonsterMoveStateMachine(states, strDown);
    }

    private async Task TempStrDownMove(IReadOnlyList<Creature> targets)
    {
        foreach (var player in Creature.CombatState.Players)
        {
            var ctx = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(ctx, player.Creature, -Amount, Creature, null);
            await PowerCmd.Apply<TenDayTempStrengthDownPower>(ctx, player.Creature, Amount, Creature, null);
        }
    }

    private async Task TempDexDownMove(IReadOnlyList<Creature> targets)
    {
        foreach (var player in Creature.CombatState.Players)
        {
            var ctx = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<DexterityPower>(ctx, player.Creature, -Amount, Creature, null);
            await PowerCmd.Apply<TenDayTempDexterityDownPower>(ctx, player.Creature, Amount, Creature, null);
        }
    }
}