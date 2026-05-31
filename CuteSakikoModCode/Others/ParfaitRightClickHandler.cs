using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interactions.RightClick;

namespace CuteSakikoMod.CuteSakikoModCode.Others;

public sealed class ParfaitRightClickHandler : IModRightClickHandler
{
    public int Priority => 100;

    public bool TryHandle(ModRightClickContext context)
    {
        if (context.Model is MatchaParfait parfait && context.Player != null)
        {
            var player = context.Player;
            if (parfait.Charges <= 0) return false;
            var combatState = player.Creature?.CombatState;
            if (combatState == null || combatState.CurrentSide != MegaCrit.Sts2.Core.Combat.CombatSide.Player) return false;

            var netService = RunManager.Instance.NetService;
            if (netService == null) return false;

            var msg = new ParfaitRightClickNetMessage { PlayerNetId = player.NetId };

            switch (netService.Type)
            {
                case NetGameType.Singleplayer:
                    parfait.ExecuteRightClickAsync(player);
                    break;
                case NetGameType.Host:
                    parfait.ExecuteRightClickAsync(player);
                    netService.SendMessage(msg);        // 主机广播给所有客户端
                    break;
                default:
                    netService.SendMessage(msg);        // 客户端发送给主机
                    break;
            }

            return true;
        }
        return false;
    }
}