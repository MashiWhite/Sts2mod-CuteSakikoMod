using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using STS2RitsuLib.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public sealed class RanaLiveManager : HookedSingletonModel
{
    public RanaLiveManager() : base(true, false) { }

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        var ownerCreature = card.Owner?.Creature;
        if (ownerCreature == null) return;

        // 检查卡牌是否拥有 RanaLive 关键词
        if (!card.Keywords.Contains(CutesakiKeywords.RanaLive.GetModCardKeyword()))
            return;

        // 如果已经拥有 LiveSweetPower，不再给予 RanaLivePower
        if (ownerCreature.HasPower<LiveSweetPower>())
            return;

        // 给予 1 层 RanaLivePower
        await PowerCmd.Apply<RanaLivePower>(choiceContext, ownerCreature, 1, ownerCreature, card);
    }
}