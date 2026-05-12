using CuteSakikoMod.CuteSakikoModCode.Events;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public class RestSiteOptionsManager : SingletonModel
{
    public override bool ShouldReceiveCombatHooks => false;

    // 不再需要 _guitar 字段

    public void BindToSynchronizer()
    {
        var sync = RunManager.Instance.RestSiteSynchronizer;
        if (sync == null) return;

        // 防止重复订阅
        sync.AfterPlayerOptionChosen -= HandleAfterPlayerOptionChosen;
        sync.AfterPlayerOptionChosen += HandleAfterPlayerOptionChosen;
    }

    private void HandleAfterPlayerOptionChosen(RestSiteOption option, bool success, ulong playerId)
    {
        if (!success || playerId != LocalContext.NetId) return;
        if (option is PracticeGuitarOption) return;

        // 根据本地玩家 ID 找到他自己的 AnonGuitar
        var me = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState());
        var myGuitar = me?.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (myGuitar == null) return;

        myGuitar.NormalOptionUsed = true;

        // 如果我有帐篷，就不禁用剩余选项
        if (me.Relics.OfType<MiniatureTent>().Any())
            return;

        // 否则禁用所有非练习选项
        var allLocalOptions = RunManager.Instance.RestSiteSynchronizer.GetLocalOptions();
        foreach (var opt in allLocalOptions)
        {
            if (!(opt is PracticeGuitarOption))
                opt.IsEnabled = false;
        }
    }
}