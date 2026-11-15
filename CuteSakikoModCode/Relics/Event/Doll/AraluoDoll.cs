using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interactions.RightClick;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event.Doll;

public class AraluoDoll : CuteSakikoEventRelic, IModRightClickableRelic
{
    private static readonly string[] UmePowerFiles =
    {
        "umepower1.mp3",
        "umepower2.mp3",
        "umepower3.mp3"
    };

    private static readonly Random _rand = new();
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<VigorPower>(); }
    }

    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        return true;
    }

    public Task OnRightClick(ModRightClickExecutionContext context)
    {
        var file = UmePowerFiles[_rand.Next(UmePowerFiles.Length)];
        var fullPath = AudioManager.ResolveAudioPath(file);
        AudioManager.PlaySound(fullPath);
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature)) return;
        if (Owner.PlayerCombatState.TurnNumber > 1) return;

        Flash();
        await PowerCmd.Apply<VigorPower>(
            choiceContext, Owner.Creature, 3m, Owner.Creature, null);
    }
}