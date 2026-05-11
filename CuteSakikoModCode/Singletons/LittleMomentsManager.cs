using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public class LittleMomentsManager : HookedSingletonModel
{
    public LittleMomentsManager() : base(true, false) { }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? source)
    {
        if (card is not LittleMoments || card.Pile?.Type != PileType.Hand)
            return;

        var owner = card.Owner;
        if (owner == null) return;

        var hand = PileType.Hand.GetPile(owner);
        if (hand == null) return;

        // 收集所有手牌中的“小小的瞬间”
        var littleMoments = hand.Cards.OfType<LittleMoments>().ToList();
        if (littleMoments.Count < 5) return;

        // 再次确认它们都还在手牌（防止当前正在打出的牌导致状态变化）
        if (littleMoments.Any(c => c.Pile?.Type != PileType.Hand))
            return;

        bool anyUpgraded = littleMoments.Any(c => c.IsUpgraded);

        // 像你的示例那样，一次性批量移除（不跳过任何特效）
        await CardPileCmd.RemoveFromCombat(littleMoments);

        // 生成合并后的“一辈子”
        var combatState = owner.Creature.CombatState;
        var lifetime = combatState.CreateCard<Lifetime>(owner);
        if (anyUpgraded)
        {
            lifetime.UpgradeInternal();
            lifetime.FinalizeUpgradeInternal();
        }

        // 正常加入手牌（带特效）
        await CardPileCmd.AddGeneratedCardToCombat(lifetime, PileType.Hand, owner);
    }
}