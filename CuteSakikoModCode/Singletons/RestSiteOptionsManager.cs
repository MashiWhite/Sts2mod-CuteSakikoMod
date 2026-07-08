using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Events;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public class RestSiteOptionsManager : SingletonModel
{
    public override bool ShouldReceiveCombatHooks => false;

    public void BindToSynchronizer()
    {
        var sync = RunManager.Instance.RestSiteSynchronizer;
        if (sync == null) return;

        sync.AfterPlayerOptionChosen -= HandleAfterPlayerOptionChosen;
        sync.AfterPlayerOptionChosen += HandleAfterPlayerOptionChosen;
    }

    private void HandleAfterPlayerOptionChosen(RestSiteOption option, bool success, ulong playerId)
    {
        if (!success) return;
        RitsuLibFramework.Logger.Info($"Option chosen: {option.OptionId}, success={success}");

        if (option is PracticeGuitarOption) return;

        var state = RunManager.Instance.DebugOnlyGetState();
        var player = state?.Players.FirstOrDefault(p => p.NetId == playerId);
        if (player == null) return;

        var guitar = player.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null)
            guitar.NormalOptionUsed = true;
    }
}