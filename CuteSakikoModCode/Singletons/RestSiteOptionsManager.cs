using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Events;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.Models.Relics;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public class RestSiteOptionsManager : SingletonModel
{
    public override bool ShouldReceiveCombatHooks => false;

    private AnonGuitar? _guitar;

    public void BindToSynchronizer(AnonGuitar guitar)
    {
        _guitar = guitar;
        var sync = RunManager.Instance.RestSiteSynchronizer;
        if (sync == null) return;

        // 先解绑，防止多次订阅
        sync.AfterPlayerOptionChosen -= HandleAfterPlayerOptionChosen;
        sync.AfterPlayerOptionChosen += HandleAfterPlayerOptionChosen;
    }

    private void HandleAfterPlayerOptionChosen(RestSiteOption option, bool success, ulong playerId)
    {
        if (!success || playerId != LocalContext.NetId) return;
        if (option is PracticeGuitarOption) return;

        // 标记吉他
        if (_guitar != null) _guitar.NormalOptionUsed = true;

        // 检查是否拥有帐篷——如果有，不限制任何选项
        if (_guitar?.Owner?.Relics.Any(r => r is MiniatureTent) == true)
            return;  // 帐篷允许无限选择，直接放行

        // 获取本地玩家的选项列表，将所有非练习选项禁用
        var allLocalOptions = RunManager.Instance.RestSiteSynchronizer.GetLocalOptions();
        foreach (var opt in allLocalOptions)
        {
            if (!(opt is PracticeGuitarOption))
                opt.IsEnabled = false;
        }
    }
}